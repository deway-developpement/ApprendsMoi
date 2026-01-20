using Microsoft.AspNetCore.Mvc;

namespace backend.Domains.Users;

[ApiController]
[Route("api/[controller]")]
public class UsersController(UserHandler handler) : ControllerBase {
    private readonly UserHandler _handler = handler;

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> Get(CancellationToken ct) {
        var list = await _handler.GetAllUsersAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct) {
        var user = await _handler.GetUserByIdAsync(id, ct);
        if (user == null) return NotFound();
        return Ok(user);
    }
}
