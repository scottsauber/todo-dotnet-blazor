using Bunit;
using Shouldly;
using Todo.Web.Components.Shared;
using Todo.Web.Models;
using Xunit;

namespace Todo.Web.Tests.Components;

public class TodoItemComponentTests : BunitContext
{
    #region Render Tests

    [Fact]
    public void Render_DisplaysTodoTitle()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Test todo title" };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        cut.Find(".todo-title").TextContent.ShouldBe("Test todo title");
    }

    [Fact]
    public void Render_IncompleteTodo_CheckboxIsNotChecked()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Incomplete", IsCompleted = false };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        checkbox.HasAttribute("checked").ShouldBeFalse();
    }

    [Fact]
    public void Render_CompletedTodo_CheckboxIsChecked()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Completed", IsCompleted = true };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        var checkbox = cut.Find("input[type='checkbox']");
        checkbox.HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void Render_DisplaysDeleteButton()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Test todo" };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        var deleteButton = cut.Find(".btn-delete");
        deleteButton.ShouldNotBeNull();
        deleteButton.GetAttribute("title").ShouldBe("Delete");
    }

    [Fact]
    public void Render_DeleteButton_ContainsTimesSymbol()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Test todo" };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        var deleteButton = cut.Find(".btn-delete");
        // The &times; entity renders as the multiplication sign character (×)
        deleteButton.TextContent.Trim().ShouldBe("\u00d7");
    }

    #endregion

    #region CSS Class Tests

    [Fact]
    public void Render_IncompleteTodo_DoesNotHaveCompletedClass()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Incomplete", IsCompleted = false };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        var listItem = cut.Find("li");
        listItem.ClassList.ShouldContain("todo-item");
        listItem.ClassList.ShouldNotContain("completed");
    }

    [Fact]
    public void Render_CompletedTodo_HasCompletedClass()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Completed", IsCompleted = true };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        var listItem = cut.Find("li");
        listItem.ClassList.ShouldContain("todo-item");
        listItem.ClassList.ShouldContain("completed");
    }

    #endregion

    #region OnToggle EventCallback Tests

    [Fact]
    public async Task OnToggle_WhenCheckboxChanged_InvokesCallbackWithCorrectId()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todoItem = new TodoItem { Id = todoId, Title = "Test todo" };
        Guid? callbackReceivedId = null;

        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem)
            .Add(p => p.OnToggle, (Guid id) => { callbackReceivedId = id; }));

        // Act
        await cut.Find("input[type='checkbox']").ChangeAsync(new());

        // Assert
        callbackReceivedId.ShouldBe(todoId);
    }

    [Fact]
    public async Task OnToggle_WhenCallbackNotProvided_DoesNotThrow()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Test todo" };

        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Act & Assert
        await Should.NotThrowAsync(async () => await cut.Find("input[type='checkbox']").ChangeAsync(new()));
    }

    #endregion

    #region OnDelete EventCallback Tests

    [Fact]
    public async Task OnDelete_WhenDeleteButtonClicked_InvokesCallbackWithCorrectId()
    {
        // Arrange
        var todoId = Guid.NewGuid();
        var todoItem = new TodoItem { Id = todoId, Title = "Test todo" };
        Guid? callbackReceivedId = null;

        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem)
            .Add(p => p.OnDelete, (Guid id) => { callbackReceivedId = id; }));

        // Act
        await cut.Find(".btn-delete").ClickAsync(new());

        // Assert
        callbackReceivedId.ShouldBe(todoId);
    }

    [Fact]
    public async Task OnDelete_WhenCallbackNotProvided_DoesNotThrow()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Test todo" };

        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Act & Assert
        await Should.NotThrowAsync(async () => await cut.Find(".btn-delete").ClickAsync(new()));
    }

    #endregion

    #region Data Binding Tests

    [Fact]
    public void Render_TodoWithSpecialCharacters_DisplaysCorrectly()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "<script>alert('xss')</script>" };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert - Blazor should encode the HTML
        cut.Find(".todo-title").TextContent.ShouldBe("<script>alert('xss')</script>");
    }

    [Fact]
    public void Render_TodoWithLongTitle_DisplaysFullTitle()
    {
        // Arrange
        var longTitle = new string('A', 1000);
        var todoItem = new TodoItem { Title = longTitle };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        cut.Find(".todo-title").TextContent.ShouldBe(longTitle);
    }

    [Fact]
    public void Render_TodoWithEmptyTitle_DisplaysEmptySpan()
    {
        // Arrange
        var todoItem = new TodoItem { Title = string.Empty };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        cut.Find(".todo-title").TextContent.ShouldBeEmpty();
    }

    #endregion

    #region Component Structure Tests

    [Fact]
    public void Render_HasCorrectStructure()
    {
        // Arrange
        var todoItem = new TodoItem { Title = "Test" };

        // Act
        var cut = Render<TodoItemComponent>(parameters => parameters
            .Add(p => p.Item, todoItem));

        // Assert
        var li = cut.Find("li.todo-item");
        li.ShouldNotBeNull();

        cut.Find("li > input[type='checkbox']").ShouldNotBeNull();
        cut.Find("li > span.todo-title").ShouldNotBeNull();
        cut.Find("li > button.btn-delete").ShouldNotBeNull();
    }

    #endregion
}
