using Todo.Web.Models;

namespace Todo.Web.Services;

public class InMemoryTodoService : ITodoService
{
    private readonly List<TodoItem> _todos = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<TodoItem>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<TodoItem>>(
                _todos.OrderByDescending(t => t.CreatedAt).ToList());
        }
    }

    public Task<TodoItem?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            return Task.FromResult(_todos.FirstOrDefault(t => t.Id == id));
        }
    }

    public Task<TodoItem> AddAsync(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));

        var item = new TodoItem { Title = title.Trim() };
        lock (_lock)
        {
            _todos.Add(item);
        }
        return Task.FromResult(item);
    }

    public Task<TodoItem?> ToggleAsync(Guid id)
    {
        lock (_lock)
        {
            var item = _todos.FirstOrDefault(t => t.Id == id);
            if (item is not null)
            {
                item.IsCompleted = !item.IsCompleted;
                item.CompletedAt = item.IsCompleted ? DateTime.UtcNow : null;
            }
            return Task.FromResult(item);
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        lock (_lock)
        {
            var item = _todos.FirstOrDefault(t => t.Id == id);
            if (item is not null)
            {
                _todos.Remove(item);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    public Task<TodoItem?> UpdateAsync(Guid id, string newTitle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newTitle, nameof(newTitle));

        lock (_lock)
        {
            var item = _todos.FirstOrDefault(t => t.Id == id);
            if (item is not null)
            {
                item.Title = newTitle.Trim();
            }
            return Task.FromResult(item);
        }
    }

    public Task ClearCompletedAsync()
    {
        lock (_lock)
        {
            _todos.RemoveAll(t => t.IsCompleted);
        }
        return Task.CompletedTask;
    }
}
