using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.DTOs;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;

        public AuthController(IAuthRepository repo, IConfiguration config)
        {
            _config = config;
            _repo = repo;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserToRegisterDto userToRegisterDto)
        {
            userToRegisterDto.Username = userToRegisterDto.Username.ToLower();

            if (await _repo.UserExists(userToRegisterDto.Username))
                return BadRequest("Username already exist!");

            var userToCreate = new User
            {
                Username = userToRegisterDto.Username
            };

            var createdUser = await _repo.Register(userToCreate, userToRegisterDto.Password);

            return StatusCode(201, createdUser);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var repoUser = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

            if (repoUser == null)
                return Unauthorized();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, repoUser.Id.ToString()),
                new Claim(ClaimTypes.Name, repoUser.Username)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _config.GetSection("AppSettings:Token").Value
                    )
                );
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new{
                token = tokenHandler.WriteToken(token)
            });
        }
    }
}
