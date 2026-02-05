using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Todo.Web.Components.Pages;
using Todo.Web.Models;
using Todo.Web.Services;
using Xunit;

namespace Todo.Web.Tests.Components;

public class TodoListTests : BunitContext
{
    private readonly ITodoService _mockTodoService;

    public TodoListTests()
    {
        _mockTodoService = Substitute.For<ITodoService>();
        Services.AddSingleton(_mockTodoService);
    }

    #region Initial Render Tests

    [Fact]
    public void InitialRender_WithNoTodos_DisplaysEmptyState()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        // Act
        var cut = Render<TodoList>();

        // Assert
        cut.Find(".empty-state").TextContent.ShouldContain("No todos yet");
    }

    [Fact]
    public void InitialRender_WithExistingTodos_DisplaysTodoItems()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "First todo" },
            new() { Id = Guid.NewGuid(), Title = "Second todo" }
        };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        // Act
        var cut = Render<TodoList>();

        // Assert
        var todoItems = cut.FindAll(".todo-item");
        todoItems.Count.ShouldBe(2);
    }

    [Fact]
    public void InitialRender_DisplaysPageTitle()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        // Act
        var cut = Render<TodoList>();

        // Assert
        cut.Find("h1").TextContent.ShouldBe("Todo List");
    }

    [Fact]
    public void InitialRender_DisplaysInputFieldWithPlaceholder()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        // Act
        var cut = Render<TodoList>();

        // Assert
        var input = cut.Find("input[type='text']");
        input.GetAttribute("placeholder").ShouldBe("What needs to be done?");
    }

    #endregion

    #region Add Button State Tests

    [Fact]
    public void AddButton_WhenInputIsEmpty_IsDisabled()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        // Act
        var cut = Render<TodoList>();

        // Assert
        var button = cut.Find("button.btn-primary");
        button.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void AddButton_WhenInputContainsOnlyWhitespace_IsDisabled()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        // Act
        var cut = Render<TodoList>();
        cut.Find("input[type='text']").Input("   ");

        // Assert
        var button = cut.Find("button.btn-primary");
        button.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void AddButton_WhenInputHasText_IsEnabled()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        // Act
        var cut = Render<TodoList>();
        cut.Find("input[type='text']").Input("New todo");

        // Assert
        var button = cut.Find("button.btn-primary");
        button.HasAttribute("disabled").ShouldBeFalse();
    }

    #endregion

    #region AddTodo Tests

    [Fact]
    public async Task AddTodo_WhenButtonClicked_CallsServiceAddAsync()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));
        _mockTodoService.AddAsync(Arg.Any<string>()).Returns(Task.FromResult(new TodoItem { Title = "New todo" }));

        var cut = Render<TodoList>();
        cut.Find("input[type='text']").Input("New todo");

        // Act
        await cut.Find("button.btn-primary").ClickAsync(new());

        // Assert
        await _mockTodoService.Received(1).AddAsync("New todo");
    }

    [Fact]
    public async Task AddTodo_AfterAdding_ClearsInputField()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));
        _mockTodoService.AddAsync(Arg.Any<string>()).Returns(Task.FromResult(new TodoItem { Title = "New todo" }));

        var cut = Render<TodoList>();
        cut.Find("input[type='text']").Input("New todo");

        // Act
        await cut.Find("button.btn-primary").ClickAsync(new());

        // Assert
        var input = cut.Find("input[type='text']");
        input.GetAttribute("value").ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task AddTodo_AfterAdding_ReloadsTodoList()
    {
        // Arrange
        var callCount = 0;
        _mockTodoService.GetAllAsync().Returns(_ =>
        {
            callCount++;
            return Task.FromResult<IReadOnlyList<TodoItem>>([]);
        });
        _mockTodoService.AddAsync(Arg.Any<string>()).Returns(Task.FromResult(new TodoItem { Title = "New todo" }));

        var cut = Render<TodoList>();
        var initialCallCount = callCount;
        cut.Find("input[type='text']").Input("New todo");

        // Act
        await cut.Find("button.btn-primary").ClickAsync(new());

        // Assert
        callCount.ShouldBeGreaterThan(initialCallCount);
    }

    #endregion

    #region HandleKeyDown Tests

    [Fact]
    public async Task HandleKeyDown_WhenEnterPressed_AddsNewTodo()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));
        _mockTodoService.AddAsync(Arg.Any<string>()).Returns(Task.FromResult(new TodoItem { Title = "New todo" }));

        var cut = Render<TodoList>();
        cut.Find("input[type='text']").Input("New todo");

        // Act
        await cut.Find("input[type='text']").KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        // Assert
        await _mockTodoService.Received(1).AddAsync("New todo");
    }

    [Fact]
    public async Task HandleKeyDown_WhenOtherKeyPressed_DoesNotAddTodo()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        var cut = Render<TodoList>();
        cut.Find("input[type='text']").Input("New todo");

        // Act
        await cut.Find("input[type='text']").KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Tab" });

        // Assert
        await _mockTodoService.DidNotReceive().AddAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task HandleKeyDown_WhenEnterPressedWithEmptyInput_DoesNotAddTodo()
    {
        // Arrange
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>([]));

        var cut = Render<TodoList>();

        // Act
        await cut.Find("input[type='text']").KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        // Assert
        await _mockTodoService.DidNotReceive().AddAsync(Arg.Any<string>());
    }

    #endregion

    #region ToggleTodo Tests

    [Fact]
    public async Task ToggleTodo_WhenCheckboxClicked_CallsServiceToggleAsync()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));
        _mockTodoService.ToggleAsync(todoId).Returns(Task.FromResult<TodoItem?>(todos[0]));

        var cut = Render<TodoList>();

        // Act
        await cut.Find("input[type='checkbox']").ChangeAsync(new());

        // Assert
        await _mockTodoService.Received(1).ToggleAsync(todoId);
    }

    [Fact]
    public async Task ToggleTodo_AfterToggling_ReloadsTodoList()
    {
        // Arrange
        var callCount = 0;
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(_ =>
        {
            callCount++;
            return Task.FromResult<IReadOnlyList<TodoItem>>(todos);
        });
        _mockTodoService.ToggleAsync(todoId).Returns(Task.FromResult<TodoItem?>(todos[0]));

        var cut = Render<TodoList>();
        var initialCallCount = callCount;

        // Act
        await cut.Find("input[type='checkbox']").ChangeAsync(new());

        // Assert
        callCount.ShouldBeGreaterThan(initialCallCount);
    }

    #endregion

    #region DeleteTodo Tests

    [Fact]
    public async Task DeleteTodo_WhenDeleteButtonClicked_ShowsConfirmationModal()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        var cut = Render<TodoList>();

        // Act
        await cut.Find(".btn-delete").ClickAsync(new());

        // Assert
        var modal = cut.Find(".delete-confirmation-modal");
        modal.ShouldNotBeNull();
        modal.QuerySelector("h3")!.TextContent.ShouldContain("Confirm Delete");
    }

    [Fact]
    public async Task DeleteTodo_WhenYesClicked_CallsServiceDeleteAsync()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));
        _mockTodoService.DeleteAsync(todoId).Returns(Task.FromResult(true));

        var cut = Render<TodoList>();

        // Act - Show modal then click Yes
        await cut.Find(".btn-delete").ClickAsync(new());
        await cut.Find(".btn-danger").ClickAsync(new());

        // Assert
        await _mockTodoService.Received(1).DeleteAsync(todoId);
    }

    [Fact]
    public async Task DeleteTodo_WhenNoClicked_DoesNotCallServiceDeleteAsync()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        var cut = Render<TodoList>();

        // Act - Show modal then click No
        await cut.Find(".btn-delete").ClickAsync(new());
        await cut.Find(".btn-secondary").ClickAsync(new());

        // Assert
        await _mockTodoService.DidNotReceive().DeleteAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteTodo_WhenNoClicked_HidesModal()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        var cut = Render<TodoList>();

        // Act - Show modal then click No
        await cut.Find(".btn-delete").ClickAsync(new());
        await cut.Find(".btn-secondary").ClickAsync(new());

        // Assert
        var modal = cut.FindAll(".delete-confirmation-modal");
        modal.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteTodo_AfterConfirmingDelete_ReloadsTodoList()
    {
        // Arrange
        var callCount = 0;
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(_ =>
        {
            callCount++;
            return Task.FromResult<IReadOnlyList<TodoItem>>(todos);
        });
        _mockTodoService.DeleteAsync(todoId).Returns(Task.FromResult(true));

        var cut = Render<TodoList>();
        var initialCallCount = callCount;

        // Act - Show modal then click Yes
        await cut.Find(".btn-delete").ClickAsync(new());
        await cut.Find(".btn-danger").ClickAsync(new());

        // Assert
        callCount.ShouldBeGreaterThan(initialCallCount);
    }

    [Fact]
    public async Task DeleteTodo_ModalHasYesAndNoButtons()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        var cut = Render<TodoList>();

        // Act
        await cut.Find(".btn-delete").ClickAsync(new());

        // Assert
        var yesButton = cut.Find(".btn-danger");
        var noButton = cut.Find(".btn-secondary");
        yesButton.TextContent.ShouldBe("Yes");
        noButton.TextContent.ShouldBe("No");
    }

    [Fact]
    public async Task DeleteTodo_WhenEscapePressed_HidesModal()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todos = new List<TodoItem> { new() { Id = todoId, Title = "Test todo" } };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        var cut = Render<TodoList>();

        // Act - Show modal then press Escape
        await cut.Find(".btn-delete").ClickAsync(new());
        await cut.Find(".delete-confirmation-modal").KeyDownAsync(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        // Assert
        var modal = cut.FindAll(".delete-confirmation-modal");
        modal.Count.ShouldBe(0);
    }

    #endregion

    #region ClearCompleted Tests

    [Fact]
    public void ClearCompletedButton_WhenNoCompletedTodos_IsNotDisplayed()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Incomplete", IsCompleted = false }
        };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        // Act
        var cut = Render<TodoList>();

        // Assert
        var clearButtons = cut.FindAll(".btn-link");
        clearButtons.Count.ShouldBe(0);
    }

    [Fact]
    public void ClearCompletedButton_WhenCompletedTodosExist_IsDisplayed()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Completed", IsCompleted = true }
        };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        // Act
        var cut = Render<TodoList>();

        // Assert
        var clearButton = cut.Find(".btn-link");
        clearButton.TextContent.ShouldContain("Clear completed");
    }

    [Fact]
    public async Task ClearCompleted_WhenClicked_CallsServiceClearCompletedAsync()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Completed", IsCompleted = true }
        };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));
        _mockTodoService.ClearCompletedAsync().Returns(Task.CompletedTask);

        var cut = Render<TodoList>();

        // Act
        await cut.Find(".btn-link").ClickAsync(new());

        // Assert
        await _mockTodoService.Received(1).ClearCompletedAsync();
    }

    #endregion

    #region Items Count Display Tests

    [Fact]
    public void ItemsCount_WithAllIncompleteTodos_DisplaysCorrectCount()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "First", IsCompleted = false },
            new() { Id = Guid.NewGuid(), Title = "Second", IsCompleted = false },
            new() { Id = Guid.NewGuid(), Title = "Third", IsCompleted = false }
        };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        // Act
        var cut = Render<TodoList>();

        // Assert
        cut.Find(".todo-stats span").TextContent.ShouldContain("3 items left");
    }

    [Fact]
    public void ItemsCount_WithMixedTodos_DisplaysOnlyIncompleteCount()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Incomplete 1", IsCompleted = false },
            new() { Id = Guid.NewGuid(), Title = "Completed", IsCompleted = true },
            new() { Id = Guid.NewGuid(), Title = "Incomplete 2", IsCompleted = false }
        };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        // Act
        var cut = Render<TodoList>();

        // Assert
        cut.Find(".todo-stats span").TextContent.ShouldContain("2 items left");
    }

    [Fact]
    public void ItemsCount_WithAllCompletedTodos_DisplaysZeroItemsLeft()
    {
        // Arrange
        var todos = new List<TodoItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Completed 1", IsCompleted = true },
            new() { Id = Guid.NewGuid(), Title = "Completed 2", IsCompleted = true }
        };
        _mockTodoService.GetAllAsync().Returns(Task.FromResult<IReadOnlyList<TodoItem>>(todos));

        // Act
        var cut = Render<TodoList>();

        // Assert
        cut.Find(".todo-stats span").TextContent.ShouldContain("0 items left");
    }

    #endregion
}
