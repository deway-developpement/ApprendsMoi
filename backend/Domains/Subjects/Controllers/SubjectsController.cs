using backend.Domains.Subjects.Services;
using backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Database.Models;

namespace backend.Domains.Subjects.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubjectsController : ControllerBase {
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService) {
        _subjectService = subjectService;
    }

    [HttpPost]
    public async Task<ActionResult<SubjectDto>> CreateSubject([FromBody] CreateSubjectDto dto) {
        try {
            var subject = await _subjectService.CreateSubjectAsync(dto);
            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subject);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectDto>> GetSubject(Guid id) {
        try {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            return Ok(subject);
        }
        catch (Exception ex) {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAllSubjects() {
        try {
            var subjects = await _subjectService.GetAllSubjectsAsync();
            return Ok(subjects);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SubjectDto>> UpdateSubject(Guid id, [FromBody] UpdateSubjectDto dto) {
        try {
            var subject = await _subjectService.UpdateSubjectAsync(id, dto);
            return Ok(subject);
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [RequireRole(ProfileType.Admin)]
    public async Task<IActionResult> DeleteSubject(Guid id) {
        try {
            await _subjectService.DeleteSubjectAsync(id);
            return NoContent();
        }
        catch (Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }
}
