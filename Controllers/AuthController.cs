using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskManagerAPI.Data;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly TaskDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(TaskDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ✅ REGISTER USER
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                return BadRequest("Username and Password are required.");

            if (_context.Users.Any(u => u.Username == model.Username))
                return BadRequest("Username already exists.");

            var user = new User
            {
                Username = model.Username,
                PasswordHash = HashPassword(model.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully!");
        }

        // ✅ LOGIN & GENERATE TOKEN
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
                return BadRequest("Username and Password are required.");

            var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);
            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = GenerateJwtToken(user.Username);
            return Ok(new { token });
        }

        // 🛠 HASH PASSWORD
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty");

            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }


        // 🛠 VERIFY PASSWORD
        private bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            return HashPassword(password) == storedHash;
        }

        // 🛠 GENERATE JWT TOKEN
        private string GenerateJwtToken(string username)
        {
            // Retrieve JWT configuration values from appsettings.json
            string? key = _config["Jwt:Key"];
            string? issuer = _config["Jwt:Issuer"];
            string? audience = _config["Jwt:Audience"]; // Add the audience

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("JWT configuration is missing.");
            }

            // Create the security key and credentials
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Define the claims for the token
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Create the JWT token
            var token = new JwtSecurityToken(
                issuer: issuer,          // Issuer (who created the token)
                audience: audience,      // Audience (who the token is for)
                claims: claims,          // Claims included in the token
                expires: DateTime.Now.AddMinutes(30), // Token expiration
                signingCredentials: credentials // Signing credentials
            );

            // Generate the token string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }

    // ✅ LOGIN MODEL
    public class LoginModel
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

}
