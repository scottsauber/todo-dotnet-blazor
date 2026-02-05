using Todo.Web.Models;

namespace Todo.Web.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync();
    Task<TodoItem?> GetByIdAsync(Guid id);
    Task<TodoItem> AddAsync(string title);
    Task<TodoItem?> ToggleAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    Task<TodoItem?> UpdateAsync(Guid id, string newTitle);
    Task ClearCompletedAsync();
}
