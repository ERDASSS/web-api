using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;

    private readonly IMapper mapper;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
            return NotFound();

        // var userDto = new UserDto
        // {
        //     Id = user.Id,
        //     CurrentGameId = user.CurrentGameId,
        //     FullName = $"{user.LastName} {user.FirstName}",
        //     Login = user.Login,
        //     GamesPlayed = user.GamesPlayed
        // };

        var userDto = mapper.Map<UserDto>(user);

        return Ok(userDto);
    }

    [Produces("application/json", "application/xml")]
    [HttpPost]
    public IActionResult CreateUser([FromBody] UserDtoForCreating? user)
    {
        if (user == null)
            return BadRequest();

        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);

        if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Логин должен состоять только из букв и цифр");
            return UnprocessableEntity(ModelState);
        }

        var userEntity = mapper.Map<UserEntity>(user);
        var createdUserEntity = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] Guid userId, [FromBody] UserDto user)
    {
        var existingUser = userRepository.FindById(userId);
        var userLastName = user.FullName.Split(' ')[0];
        var userFirstName = user.FullName.Split(' ')[1];

        if (string.IsNullOrWhiteSpace(user.FullName) || user.FullName.Split(' ').Length < 2)
            return UnprocessableEntity();


        if (existingUser == null)
        {
            var userEntity = mapper.Map<UserEntity>(user);
            userEntity.LastName = userLastName;
            userEntity.FirstName = userFirstName;
            // var newUser = new UserEntity(userId, user.Login, userLastName, userFirstName, user.GamesPlayed, user.CurrentGameId);
            userRepository.UpdateOrInsert(userEntity, out var isInserted);

            var updatedUserDto = mapper.Map<UserDto>(userEntity);
            return CreatedAtAction(nameof(GetUserById), new { userId = userId }, updatedUserDto);
        }
        else
        {
            mapper.Map(user, existingUser);
            existingUser.LastName = userLastName;
            existingUser.FirstName = userFirstName;
            userRepository.UpdateOrInsert(existingUser, out var isInserted);

            var updatedUserDto = mapper.Map<UserDto>(existingUser);
            return Ok(updatedUserDto);
        }
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UserDtoForUpdating>? patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();

        if (!Guid.TryParse(userId, out var userGuid))
            return NotFound();

        var existingUser = userRepository.FindById(userGuid);
        if (existingUser == null)
            return NotFound();

        var userToPatch = mapper.Map<UserDtoForUpdating>(existingUser);

        patchDoc.ApplyTo(userToPatch, ModelState);

        TryValidateModel(userToPatch);

        if (!ModelState.IsValid)
        {
            return new ObjectResult(ModelState)
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                ContentTypes = { "application/json" }
            };
        }

        mapper.Map(userToPatch, existingUser);

        userRepository.UpdateOrInsert(existingUser, out var isInserted);

        return NoContent();
    }
}