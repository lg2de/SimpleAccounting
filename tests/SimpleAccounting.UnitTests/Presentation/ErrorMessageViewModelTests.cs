// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Presentation;

using FluentAssertions;
using lg2de.SimpleAccounting.Abstractions;
using lg2de.SimpleAccounting.Presentation;
using NSubstitute;
using Xunit;

public class ErrorMessageViewModelTests
{
    [Fact]
    public void Commands_HappyPath_CanBeExecuted()
    {
        var processApi = Substitute.For<IProcess>();
        var clipboard = Substitute.For<IClipboard>();
        var sut = new ErrorMessageViewModel(processApi, clipboard);

        sut.ClipboardCommand.CanExecute(null).Should().BeTrue();
        sut.ReportGitHubCommand.CanExecute(null).Should().BeTrue();
        sut.ReportEmailCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ClipboardCommand_SampleInput_ClipboardTextFormatted()
    {
        var processApi = Substitute.For<IProcess>();
        var clipboard = Substitute.For<IClipboard>();
        var clipboardText = string.Empty;
        clipboard.When(x => x.SetText(Arg.Any<string>())).Do(x => clipboardText = x.Arg<string>());
        var sut = new ErrorMessageViewModel(processApi, clipboard)
        {
            CallStack = "call-stack", Introduction = "intro-duction", ErrorMessage = "error-message"
        };

        sut.ClipboardCommand.Execute(null);

        clipboardText.Should().Match(
            """
            error-message
            call-stack


            Version: *
            """);
    }

    [Fact]
    public void ReportGitHubCommand_HappyPath_UriInvoked()
    {
        var processApi = Substitute.For<IProcess>();
        var clipboard = Substitute.For<IClipboard>();
        var sut = new ErrorMessageViewModel(processApi, clipboard);

        sut.ReportGitHubCommand.Execute(null);

        processApi.Received(1).ShellExecute(Arg.Any<string>());
    }

    [Fact]
    public void ReportEmailCommand_HappyPath_UriInvoked()
    {
        var processApi = Substitute.For<IProcess>();
        var clipboard = Substitute.For<IClipboard>();
        var sut = new ErrorMessageViewModel(processApi, clipboard);

        sut.ReportEmailCommand.Execute(null);

        processApi.Received(1).ShellExecute(Arg.Any<string>());
    }

    [Fact]
    public void FullErrorText_SampleInput_Formatted()
    {
        var processApi = Substitute.For<IProcess>();
        var clipboard = Substitute.For<IClipboard>();
        var sut = new ErrorMessageViewModel(processApi, clipboard)
        {
            CallStack = "call-stack", Introduction = "intro-duction", ErrorMessage = "error-message"
        };

        sut.FullErrorText.Should().Be("intro-duction\r\nerror-message");
    }
}
