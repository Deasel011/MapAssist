// See https://aka.ms/new-console-template for more information

using MapAssist.Helpers;
using MapAssist.Structs;
using MapAssist.Types;
using Spectre.Console;
using System.Diagnostics;
using System.Text;
using Session = MapAssist.Types.Session;
using UnitAny = MapAssist.Structs.UnitAny;

// ReSharper disable SuggestVarOrType_SimpleTypes

// ReSharper disable SuggestVarOrType_Elsewhere

Dictionary<int, IntPtr> _UnitHashTableOffset = new Dictionary<int, IntPtr>();
Dictionary<int, IntPtr> _ExpansionCheckOffset = new Dictionary<int, IntPtr>();
Dictionary<int, IntPtr> _GameNameOffset = new Dictionary<int, IntPtr>();
Dictionary<int, IntPtr> _MenuDataOffset = new Dictionary<int, IntPtr>();
Dictionary<int, IntPtr> _RosterDataOffset = new Dictionary<int, IntPtr>();
Dictionary<int, IntPtr> _InteractedNpcOffset = new Dictionary<int, IntPtr>();
Dictionary<int, IntPtr> _LastHoverDataOffset = new Dictionary<int, IntPtr>();
Dictionary<int, IntPtr> _PetsOffsetOffset = new Dictionary<int, IntPtr>();

AnsiConsole.MarkupLine("[bold green]D2MemScanner[/] - [bold yellow]v{0}[/]",
    typeof(Program).Assembly.GetName().Version);
var d2rProcesses = Process.GetProcessesByName("D2R");
AnsiConsole.MarkupLine($"Found [bold yellow]{d2rProcesses.Length}[/] Diablo II: Resurrected processes");
if (d2rProcesses.Length > 1)
{
    AnsiConsole.MarkupLine("Multiple Diablo II: Resurrected processes found, please close all but one");
    Environment.Exit(0);
    return;
}

var PID = d2rProcesses.First().Id;
_UnitHashTableOffset[PID] = IntPtr.Zero;
_ExpansionCheckOffset[PID] = IntPtr.Zero;
_GameNameOffset[PID] = IntPtr.Zero;
_MenuDataOffset[PID] = IntPtr.Zero;
_RosterDataOffset[PID] = IntPtr.Zero;
_InteractedNpcOffset[PID] = IntPtr.Zero;
_LastHoverDataOffset[PID] = IntPtr.Zero;
_PetsOffsetOffset[PID] = IntPtr.Zero;

AnsiConsole.MarkupLine($"Found Diablo II: Resurrected process with PID [bold yellow]{PID}[/]");


var res = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .AddChoices("GameName", "ExpansionCheck", "UnitHashTable", "MenuData", "RosterData", "InteractedNpc",
            "LastHoverData", "PetsOffset", "TestAll")
        .Title("Select a value to scan for")
        .PageSize(8));

switch (res)
{
    case "GameName":
        GetGameName(PID);
        break;
    case "UnitHashTable":
        GetUnitHashTable(PID);
        break;
    case "TestAll":
        break;
    default:
        GetUnitHashTable(PID);
        break;
}


using (var processContext = new ProcessContext(Process.GetProcessById(PID)))
{
    GameManager.SetProcessContext(processContext);
    
    var pid = processContext.ProcessId;
    var buffer = processContext.Read<byte>(processContext.BaseAddr, processContext.ModuleSize);

    if (_UnitHashTableOffset[pid] == IntPtr.Zero)
    {
        _UnitHashTableOffset[pid] = processContext.GetUnitHashtableOffset(buffer);
        AnsiConsole.MarkupLine(
            $"Found offset {nameof(_UnitHashTableOffset)} 0x{_UnitHashTableOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
        var unitHashTable = processContext.Read<UnitHashTable>(IntPtr.Add(_UnitHashTableOffset[pid], 0));
        var unitPlayers = unitHashTable.UnitTable.Where(ptr => ptr != IntPtr.Zero).Select(ptr => new UnitPlayer(ptr));
        foreach (UnitPlayer unitPlayer in unitPlayers)
        {
            unitPlayer.Update();
        }

        AnsiConsole.MarkupLine($"Found [bold yellow]{unitPlayers.Count()}[/] players");
    }

    if (_ExpansionCheckOffset[pid] == IntPtr.Zero)
    {
        _ExpansionCheckOffset[pid] = processContext.GetExpansionOffset(buffer);
        AnsiConsole.MarkupLine(
            $"Found offset {nameof(_ExpansionCheckOffset)} 0x{_ExpansionCheckOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
    }

    if (_GameNameOffset[pid] == IntPtr.Zero)
    {
        _GameNameOffset[pid] = processContext.GetGameNameOffset(buffer);
        AnsiConsole.MarkupLine(
            $"Found offset {nameof(_GameNameOffset)} 0x{_GameNameOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");

        var session = new Session(_GameNameOffset[pid], processContext);

        if (session.GameName != null)
            AnsiConsole.MarkupLine($"GameName: [bold yellow]{session.GameName}[/]");
    }

    if (_MenuDataOffset[pid] == IntPtr.Zero)
    {
        _MenuDataOffset[pid] = processContext.GetMenuDataOffset(buffer);
        AnsiConsole.MarkupLine(
            $"Found offset {nameof(_MenuDataOffset)} 0x{_MenuDataOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
    }

    if (_RosterDataOffset[pid] == IntPtr.Zero)
    {
        _RosterDataOffset[pid] = processContext.GetRosterDataOffset(buffer);
        AnsiConsole.MarkupLine(
            $"Found offset {nameof(_RosterDataOffset)} 0x{_RosterDataOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
    }

    if (_LastHoverDataOffset[pid] == IntPtr.Zero)
    {
        _LastHoverDataOffset[pid] = processContext.GetLastHoverObjectOffset(buffer);
        AnsiConsole.MarkupLine(
            $"Found offset {nameof(_LastHoverDataOffset)} 0x{_LastHoverDataOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
    }

    if (_InteractedNpcOffset[pid] == IntPtr.Zero)
    {
        _InteractedNpcOffset[pid] = processContext.GetInteractedNpcOffset(buffer);
        AnsiConsole.MarkupLine(
            $"Found offset {nameof(_InteractedNpcOffset)} 0x{_InteractedNpcOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
    }

    if (_PetsOffsetOffset[pid] == IntPtr.Zero)
    {
        _PetsOffsetOffset[pid] = processContext.GetPetsOffset(buffer);
        AnsiConsole.MarkupLine(
            $"Found offset {nameof(_PetsOffsetOffset)} 0x{_PetsOffsetOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
    }
}

AnsiConsole.Prompt(new TextPrompt<string>("Press any key to exit"));

void GetGameName(int pid1)
{
//Prompt for a name
    var gameName = AnsiConsole.Ask<string>("Enter the game name to search for: ");
    var previousltFoundIntPtr = IntPtr.Zero;
    if (AnsiConsole.Prompt(new ConfirmationPrompt("Do you have a previous int pointer offset?")))
    {
        previousltFoundIntPtr = IntPtr.Parse(AnsiConsole.Ask<string>("Enter the offset: "));
    }

    AnsiConsole.Status()
        .Spinner(Spinner.Known.Star)
        .SpinnerStyle(Style.Parse("green bold"))
        .Start("Finding Game Name Offset...", ctx =>
        {
            using (var processContext = new ProcessContext(Process.GetProcessById(pid1)))
            {
                var buffer = processContext.Read<byte>(processContext.BaseAddr, processContext.ModuleSize);
                var gameNamePointer = IntPtr.Zero;
                var cancellationTokenSource = new CancellationTokenSource();
                var shownId = 0;
                var bufferPosition = 0;
                try
                {
                    var startingOffset = (previousltFoundIntPtr == IntPtr.Zero
                        ? 0
                        : (int)((previousltFoundIntPtr.ToInt64() - processContext.BaseAddr.ToInt64())));
                    Parallel.For(startingOffset, buffer.Length,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = 100, CancellationToken = cancellationTokenSource.Token
                        },
                        i =>
                        {
                            if (i > shownId)
                            {
                                shownId = i;
                                ctx.Status(
                                    $"Finding Game Name Offset... {i}/{buffer.Length} --> {Math.Floor((i / (double)buffer.Length * 100))}%");
                            }

                            var currentPointer = IntPtr.Add(processContext.BaseAddr, i);
                            try
                            {
                                var sess = processContext.Read<MapAssist.Structs.Session>(currentPointer);
                                if (Encoding.UTF8.GetString(sess.GameName)
                                    .Equals(gameName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    gameNamePointer = currentPointer;
                                    bufferPosition = i;
                                    cancellationTokenSource.Cancel();
                                }
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[bold red]Error[/] - {ex.Message}");
                            }
                        });
                }
                catch (OperationCanceledException ex)
                {
                    AnsiConsole.MarkupLine($"Found Game Name Offset: [bold yellow]{gameNamePointer}[/]");
                }

                AnsiConsole.MarkupLine($"Found buffer position: [bold yellow]{bufferPosition}[/]");
                AnsiConsole.MarkupLine($"Printing buffer area 300 before and 300 into found struct:");
                var newBytes = new byte[300];
                Buffer.BlockCopy(buffer, bufferPosition - 300, newBytes, 0, 300);
                AnsiConsole.MarkupLine("Before:");
                AnsiConsole.MarkupLine(BitConverter.ToString(newBytes));
                Buffer.BlockCopy(buffer, bufferPosition, newBytes, 0, 300);
                AnsiConsole.MarkupLine("After:");
                AnsiConsole.MarkupLine(BitConverter.ToString(newBytes));
            }
        });
}

void GetUnitHashTable(int pid1)
{
    AnsiConsole.Prompt(
        new ConfirmationPrompt("Confirm when you are close to Charsi in Act 1."));
        // new SelectionPrompt<string>()
        //     .AddChoices("Amazon", "Assassin", "Barbarian", "Druid", "Necromancer", "Paladin", "Sorceress")
        //     .Title("What is your class?")
        //     .PageSize(8));
    // var previouslyFoundPlayerUnitPtr = IntPtr.Zero;
    // if (AnsiConsole.Prompt(new ConfirmationPrompt("Do you have a previous int pointer offset for the player?")))
    // {
    //     previouslyFoundPlayerUnitPtr = IntPtr.Parse(AnsiConsole.Ask<string>("Enter the offset: "));
    // }

    var previouslyFoundHashTablePtr = IntPtr.Zero;
    if (AnsiConsole.Prompt(new ConfirmationPrompt("Do you have a previous int pointer offset for the hashTable?")))
    {
        previouslyFoundHashTablePtr = IntPtr.Parse(AnsiConsole.Ask<string>("Enter the offset: "));
    }

    AnsiConsole.Status()
        .Spinner(Spinner.Known.Star)
        .SpinnerStyle(Style.Parse("green bold"))
        .Start("Finding Unit HashTable Offset...", ctx =>
        {
            using (var processContext = new ProcessContext(Process.GetProcessById(pid1)))
            {
                var buffer = processContext.Read<byte>(processContext.BaseAddr, processContext.ModuleSize);
                var playerPointer = IntPtr.Zero;
                var hashTablePointer = IntPtr.Zero;
                var cancellationTokenSourceForPlayer = new CancellationTokenSource();
                var cancellationTokenSourceForHashTable = new CancellationTokenSource();
                var shownId = 0;
                var bufferPosition = 0;

                // Finding player class and unit
                // try
                // {
                //     var startingOffset = (previouslyFoundPlayerUnitPtr == IntPtr.Zero
                //         ? 0
                //         : (int)((previouslyFoundPlayerUnitPtr.ToInt64() - processContext.BaseAddr.ToInt64())));
                //     Parallel.For(startingOffset, buffer.Length,
                //         new ParallelOptions
                //         {
                //             MaxDegreeOfParallelism = 100, CancellationToken = cancellationTokenSourceForPlayer.Token
                //         },
                //         i =>
                //         {
                //             if (i > shownId)
                //             {
                //                 shownId = i;
                //                 ctx.Status(
                //                     $"Finding Player Unit Pointer Offset... {i}/{buffer.Length} --> {Math.Floor((i / (double)buffer.Length * 100))}%");
                //             }
                //
                //             var currentPointer = IntPtr.Add(processContext.BaseAddr, i);
                //             try
                //             {
                //                 //Finding player
                //                 var player = processContext.Read<MapAssist.Structs.UnitAny>(currentPointer);
                //                 if (player.playerClass.ToString().Equals(@class,
                //                         StringComparison.InvariantCultureIgnoreCase) && !player.isCorpse &&
                //                     player.UnitType == UnitType.Player)
                //                 {
                //                     playerPointer = currentPointer;
                //                     cancellationTokenSourceForPlayer.Cancel();
                //                 }
                //             }
                //             catch (Exception ex)
                //             {
                //                 AnsiConsole.MarkupLine($"[bold red]Error[/] - {ex.Message}");
                //             }
                //         });
                // }
                // catch (OperationCanceledException ex)
                // {
                //     AnsiConsole.MarkupLine($"Found Player Unit Pointer Offset: [bold yellow]{playerPointer}[/]");
                // }

                // Finding the HashTable
                // Start by finding the pointer address to the player
                try
                {
                    var startingOffset = (previouslyFoundHashTablePtr == IntPtr.Zero
                        ? 0
                        : (int)((previouslyFoundHashTablePtr.ToInt64() - processContext.BaseAddr.ToInt64())));
                    shownId = 0;
                    Parallel.For(startingOffset, buffer.Length,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = 100,
                            CancellationToken = cancellationTokenSourceForHashTable.Token
                        },
                        i =>
                        {
                            if (i > shownId)
                            {
                                shownId = i;
                                ctx.Status(
                                    $"Finding Unit HashTable Offset... {i}/{buffer.Length} --> {Math.Floor((i / (double)buffer.Length * 100))}%");
                            }

                            var currentPointer = IntPtr.Add(processContext.BaseAddr, i);
                            try
                            {
                                var hashTable = processContext.Read<MapAssist.Structs.UnitHashTable>(currentPointer);
                                
                                var first = hashTable.UnitTable[0];

                                if (first != IntPtr.Zero)
                                {
                                    var firstUnit = processContext.Read<MapAssist.Structs.UnitAny>(first);
                                    if (firstUnit.UnitType == UnitType.Player && firstUnit.playerClass > 0 &&
                                        hashTable.UnitTable.Any(ptr =>
                                            processContext.Read<MapAssist.Structs.UnitAny>(ptr).playerClass.ToString()
                                                .Equals(@class, StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        AnsiConsole.MarkupLine(
                                            $"Found offset: [bold yellow]{currentPointer.ToInt64() - processContext.BaseAddr.ToInt64():X}[/]");
                                        hashTablePointer = currentPointer;
                                        bufferPosition = i;
                                        cancellationTokenSourceForHashTable.Cancel();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[bold red]Error[/] - {ex.Message}");
                            }
                        });
                }
                catch (OperationCanceledException ex)
                {
                    AnsiConsole.MarkupLine($"Found Unit HashTable Offset: [bold yellow]{hashTablePointer}[/]");
                }

                AnsiConsole.MarkupLine($"Found buffer position: [bold yellow]{bufferPosition}[/]");
                AnsiConsole.MarkupLine($"Printing buffer area 300 before and 300 into found struct:");
                var newBytes = new byte[300];
                Buffer.BlockCopy(buffer, bufferPosition - 300, newBytes, 0, 300);
                AnsiConsole.MarkupLine("Before:");
                AnsiConsole.MarkupLine(BitConverter.ToString(newBytes));
                Buffer.BlockCopy(buffer, bufferPosition, newBytes, 0, 300);
                AnsiConsole.MarkupLine("After:");
                AnsiConsole.MarkupLine(BitConverter.ToString(newBytes));
            }
        });
}
