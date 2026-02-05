using Shouldly;
using Todo.Web.Models;
using Todo.Web.Services;
using Xunit;

namespace Todo.Web.Tests.Services;

public class InMemoryTodoServiceTests
{
    private readonly InMemoryTodoService _sut;

    public InMemoryTodoServiceTests()
    {
        _sut = new InMemoryTodoService();
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidTitle_ReturnsTodoItemWithCorrectTitle()
    {
        // Arrange
        const string title = "Buy groceries";

        // Act
        var result = await _sut.AddAsync(title);

        // Assert
        result.Title.ShouldBe(title);
    }

    [Fact]
    public async Task AddAsync_WithValidTitle_GeneratesUniqueId()
    {
        // Act
        var result = await _sut.AddAsync("Test todo");

        // Assert
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task AddAsync_WithValidTitle_SetsCreatedAtToRecentTime()
    {
        // Arrange
        var beforeAdd = DateTime.UtcNow;

        // Act
        var result = await _sut.AddAsync("Test todo");

        // Assert
        result.CreatedAt.ShouldBeGreaterThanOrEqualTo(beforeAdd);
        result.CreatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task AddAsync_WithValidTitle_SetsIsCompletedToFalse()
    {
        // Act
        var result = await _sut.AddAsync("Test todo");

        // Assert
        result.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task AddAsync_WithValidTitle_SetsCompletedAtToNull()
    {
        // Act
        var result = await _sut.AddAsync("Test todo");

        // Assert
        result.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_WithTitleContainingWhitespace_TrimsTitleBeforeSaving()
    {
        // Act
        var result = await _sut.AddAsync("  Buy groceries  ");

        // Assert
        result.Title.ShouldBe("Buy groceries");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task AddAsync_WithNullOrWhitespaceTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () => await _sut.AddAsync(invalidTitle!));
    }

    [Fact]
    public async Task AddAsync_CalledMultipleTimes_AddsTodosToList()
    {
        // Act
        await _sut.AddAsync("First todo");
        await _sut.AddAsync("Second todo");
        await _sut.AddAsync("Third todo");
        var allTodos = await _sut.GetAllAsync();

        // Assert
        allTodos.Count.ShouldBe(3);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenNoTodosExist_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleTodos_ReturnsAllTodos()
    {
        // Arrange
        await _sut.AddAsync("First");
        await _sut.AddAsync("Second");

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleTodos_ReturnsTodosOrderedByCreatedAtDescending()
    {
        // Arrange
        var first = await _sut.AddAsync("First");
        await Task.Delay(10); // Ensure different CreatedAt times
        var second = await _sut.AddAsync("Second");
        await Task.Delay(10);
        var third = await _sut.AddAsync("Third");

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Count.ShouldBe(3);
        result[0].Id.ShouldBe(third.Id); // Newest first
        result[1].Id.ShouldBe(second.Id);
        result[2].Id.ShouldBe(first.Id); // Oldest last
    }

    [Fact]
    public async Task GetAllAsync_ReturnsReadOnlyList()
    {
        // Arrange
        await _sut.AddAsync("Test");

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.ShouldBeAssignableTo<IReadOnlyList<TodoItem>>();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsTodoItem()
    {
        // Arrange
        var added = await _sut.AddAsync("Test todo");

        // Act
        var result = await _sut.GetByIdAsync(added.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(added.Id);
        result.Title.ShouldBe("Test todo");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        await _sut.AddAsync("Existing todo");

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.Empty);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region ToggleAsync Tests

    [Fact]
    public async Task ToggleAsync_WithExistingIncompleteTodo_SetsIsCompletedToTrue()
    {
        // Arrange
        var todo = await _sut.AddAsync("Test todo");

        // Act
        var result = await _sut.ToggleAsync(todo.Id);

        // Assert
        result.ShouldNotBeNull();
        result.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ToggleAsync_WithExistingIncompleteTodo_SetsCompletedAtToCurrentTime()
    {
        // Arrange
        var todo = await _sut.AddAsync("Test todo");
        var beforeToggle = DateTime.UtcNow;

        // Act
        var result = await _sut.ToggleAsync(todo.Id);

        // Assert
        result!.CompletedAt.ShouldNotBeNull();
        result.CompletedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeToggle);
        result.CompletedAt.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task ToggleAsync_WithExistingCompleteTodo_SetsIsCompletedToFalse()
    {
        // Arrange
        var todo = await _sut.AddAsync("Test todo");
        await _sut.ToggleAsync(todo.Id); // Complete it first

        // Act
        var result = await _sut.ToggleAsync(todo.Id); // Toggle back to incomplete

        // Assert
        result.ShouldNotBeNull();
        result.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task ToggleAsync_WithExistingCompleteTodo_SetsCompletedAtToNull()
    {
        // Arrange
        var todo = await _sut.AddAsync("Test todo");
        await _sut.ToggleAsync(todo.Id); // Complete it first

        // Act
        var result = await _sut.ToggleAsync(todo.Id); // Toggle back

        // Assert
        result!.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task ToggleAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _sut.ToggleAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ToggleAsync_PersistsChangeInStorage()
    {
        // Arrange
        var todo = await _sut.AddAsync("Test todo");

        // Act
        await _sut.ToggleAsync(todo.Id);
        var retrieved = await _sut.GetByIdAsync(todo.Id);

        // Assert
        retrieved!.IsCompleted.ShouldBeTrue();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var todo = await _sut.AddAsync("Test todo");

        // Act
        var result = await _sut.DeleteAsync(todo.Id);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_RemovesTodoFromList()
    {
        // Arrange
        var todo = await _sut.AddAsync("Test todo");

        // Act
        await _sut.DeleteAsync(todo.Id);
        var allTodos = await _sut.GetAllAsync();

        // Assert
        allTodos.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_OnlyRemovesSpecifiedTodo()
    {
        // Arrange
        var todo1 = await _sut.AddAsync("First");
        var todo2 = await _sut.AddAsync("Second");

        // Act
        await _sut.DeleteAsync(todo1.Id);
        var allTodos = await _sut.GetAllAsync();

        // Assert
        allTodos.Count.ShouldBe(1);
        allTodos[0].Id.ShouldBe(todo2.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_CalledTwiceWithSameId_ReturnsFalseOnSecondCall()
    {
        // Arrange
        var todo = await _sut.AddAsync("Test todo");

        // Act
        var firstDelete = await _sut.DeleteAsync(todo.Id);
        var secondDelete = await _sut.DeleteAsync(todo.Id);

        // Assert
        firstDelete.ShouldBeTrue();
        secondDelete.ShouldBeFalse();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingIdAndValidTitle_UpdatesTitle()
    {
        // Arrange
        var todo = await _sut.AddAsync("Original title");

        // Act
        var result = await _sut.UpdateAsync(todo.Id, "Updated title");

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe("Updated title");
    }

    [Fact]
    public async Task UpdateAsync_WithTitleContainingWhitespace_TrimsTitleBeforeSaving()
    {
        // Arrange
        var todo = await _sut.AddAsync("Original");

        // Act
        var result = await _sut.UpdateAsync(todo.Id, "  Updated  ");

        // Assert
        result!.Title.ShouldBe("Updated");
    }

    [Fact]
    public async Task UpdateAsync_PreservesOtherProperties()
    {
        // Arrange
        var todo = await _sut.AddAsync("Original");
        await _sut.ToggleAsync(todo.Id);
        var originalId = todo.Id;
        var originalCreatedAt = todo.CreatedAt;

        // Act
        var result = await _sut.UpdateAsync(todo.Id, "Updated");

        // Assert
        result!.Id.ShouldBe(originalId);
        result.CreatedAt.ShouldBe(originalCreatedAt);
        result.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _sut.UpdateAsync(Guid.NewGuid(), "New title");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_WithNullOrWhitespaceTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Arrange
        var todo = await _sut.AddAsync("Original");

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () => await _sut.UpdateAsync(todo.Id, invalidTitle!));
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangeInStorage()
    {
        // Arrange
        var todo = await _sut.AddAsync("Original");

        // Act
        await _sut.UpdateAsync(todo.Id, "Updated");
        var retrieved = await _sut.GetByIdAsync(todo.Id);

        // Assert
        retrieved!.Title.ShouldBe("Updated");
    }

    #endregion

    #region ClearCompletedAsync Tests

    [Fact]
    public async Task ClearCompletedAsync_WithNoCompletedTodos_DoesNotRemoveAnything()
    {
        // Arrange
        await _sut.AddAsync("First");
        await _sut.AddAsync("Second");

        // Act
        await _sut.ClearCompletedAsync();
        var allTodos = await _sut.GetAllAsync();

        // Assert
        allTodos.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ClearCompletedAsync_WithAllCompletedTodos_RemovesAllTodos()
    {
        // Arrange
        var todo1 = await _sut.AddAsync("First");
        var todo2 = await _sut.AddAsync("Second");
        await _sut.ToggleAsync(todo1.Id);
        await _sut.ToggleAsync(todo2.Id);

        // Act
        await _sut.ClearCompletedAsync();
        var allTodos = await _sut.GetAllAsync();

        // Assert
        allTodos.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearCompletedAsync_WithMixedTodos_RemovesOnlyCompletedTodos()
    {
        // Arrange
        var incomplete1 = await _sut.AddAsync("Incomplete 1");
        var completed = await _sut.AddAsync("Completed");
        var incomplete2 = await _sut.AddAsync("Incomplete 2");
        await _sut.ToggleAsync(completed.Id);

        // Act
        await _sut.ClearCompletedAsync();
        var allTodos = await _sut.GetAllAsync();

        // Assert
        allTodos.Count.ShouldBe(2);
        allTodos.ShouldContain(t => t.Id == incomplete1.Id);
        allTodos.ShouldContain(t => t.Id == incomplete2.Id);
        allTodos.ShouldNotContain(t => t.Id == completed.Id);
    }

    [Fact]
    public async Task ClearCompletedAsync_WithEmptyList_DoesNotThrow()
    {
        // Act & Assert
        await Should.NotThrowAsync(async () => await _sut.ClearCompletedAsync());
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentAddOperations_AllItemsAreAdded()
    {
        // Arrange
        const int itemCount = 100;
        var tasks = Enumerable.Range(0, itemCount)
            .Select(i => _sut.AddAsync($"Todo {i}"))
            .ToArray();

        // Act
        await Task.WhenAll(tasks);
        var allTodos = await _sut.GetAllAsync();

        // Assert
        allTodos.Count.ShouldBe(itemCount);
    }

    [Fact]
    public async Task ConcurrentToggleOperations_AllTogglesSucceed()
    {
        // Arrange
        var todos = new List<TodoItem>();
        for (int i = 0; i < 50; i++)
        {
            todos.Add(await _sut.AddAsync($"Todo {i}"));
        }

        // Act
        var toggleTasks = todos.Select(t => _sut.ToggleAsync(t.Id)).ToArray();
        await Task.WhenAll(toggleTasks);
        var allTodos = await _sut.GetAllAsync();

        // Assert
        allTodos.ShouldAllBe(t => t.IsCompleted);
    }

    #endregion
}
