using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
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

    [Produces("application/json", "application/xml")]
    [HttpPut("{userId}")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto user)
    {
        if (user == null)
            return BadRequest();
        
        if (!Guid.TryParse(userId, out var parsedUserId))
            return BadRequest();
        
        if (string.IsNullOrEmpty(user.Login))
            return UnprocessableEntity(JObject.FromObject(new { login = new JArray("Login is required") }));

        if (!Regex.IsMatch(user.Login, @"^[a-zA-Z0-9]+$"))
            return UnprocessableEntity(JObject.FromObject(new { login = new JArray("Login contains invalid characters") }));

        if (string.IsNullOrEmpty(user.FirstName))
            return UnprocessableEntity(JObject.FromObject(new { firstName = new JArray("First name is required") }));

        if (string.IsNullOrEmpty(user.LastName))
            return UnprocessableEntity(JObject.FromObject(new { lastName = new JArray("Last name is required") }));

        var existingUser = userRepository.FindById(parsedUserId);

        if (existingUser == null)
        {
            var userEntity = new UserEntity(parsedUserId)
            {
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                GamesPlayed = 0,
                CurrentGameId = null
            };

            userRepository.UpdateOrInsert(userEntity, out var isInserted);
            
            var createdId = userEntity.Id.ToString();
            
            return CreatedAtAction(nameof(GetUserById), new { userId = createdId }, createdId);
        }
        else
        {
            existingUser.Login = user.Login;
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;

            userRepository.UpdateOrInsert(existingUser, out var isInserted);
            
            return NoContent();
        }
    }

    [Produces("application/json", "application/xml")]
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);

        if (user == null)
            return NotFound();
        else
        {
            userRepository.Delete(userId);
            return NoContent();
        }
    }
}