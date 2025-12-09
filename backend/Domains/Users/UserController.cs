using Microsoft.AspNetCore.Mvc;

namespace backend.Domains.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController(UserHandler handler) : ControllerBase {
    private readonly UserHandler _handler = handler;

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> Get(CancellationToken ct) {
        var list = await _handler.GetAllAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct) {
        var user = await _handler.GetByIdAsync(id, ct);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest req, CancellationToken ct) {
        if (req == null || string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username is required");

        var created = await _handler.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
