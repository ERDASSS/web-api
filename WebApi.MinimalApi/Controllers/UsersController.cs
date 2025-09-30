using AutoMapper;
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
    [HttpGet("{userId}")]
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

    [HttpPost]
    public IActionResult CreateUser([FromBody] object user)
    {
        throw new NotImplementedException();
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
}