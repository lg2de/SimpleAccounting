// <copyright>
//     Copyright (c) Lukas Grützmacher. All rights reserved.
// </copyright>

namespace lg2de.SimpleAccounting.UnitTests.Infrastructure;

using System.Threading.Tasks;
using lg2de.SimpleAccounting.Infrastructure;
using NSubstitute;
using Xunit;

public class AsyncCommandTests
{
    [Fact]
    public void Execute_CommandExecuted_BusySetAndReset()
    {
        var busy = Substitute.For<IBusy>();
        var sut = new AsyncCommand(busy, () => Task.CompletedTask);
        
        sut.Execute(null);
        
        busy.Received(1).IsBusy = true;
        busy.Received(1).IsBusy = false;
    }

    [Fact]
    public async Task ExecuteAsync_CommandExecuted_BusySetAndReset()
    {
        var busy = Substitute.For<IBusy>();
        var sut = new AsyncCommand(busy, () => Task.CompletedTask);
        
        await sut.ExecuteAsync(null);
        
        busy.Received(1).IsBusy = true;
        busy.Received(1).IsBusy = false;
    }
    
    [Fact]
    public void Execute_BusyAlreadyActive_BusySetAndReset()
    {
        var busy = Substitute.For<IBusy>();
        busy.IsBusy.Returns(true);
        var sut = new AsyncCommand(busy, () => Task.CompletedTask);
        
        sut.Execute(null);
        
        busy.DidNotReceive().IsBusy = Arg.Any<bool>();
    }

    [Fact]
    public async Task ExecuteAsync_BusyAlreadyActive_BusySetAndReset()
    {
        var busy = Substitute.For<IBusy>();
        busy.IsBusy.Returns(true);
        var sut = new AsyncCommand(busy, () => Task.CompletedTask);
        
        await sut.ExecuteAsync(null);
        
        busy.DidNotReceive().IsBusy = Arg.Any<bool>();
    }
}
