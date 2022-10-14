// See https://aka.ms/new-console-template for more information

using MapAssist.Helpers;
using MapAssist.Structs;
using MapAssist.Types;
using Spectre.Console;
using System.Diagnostics;
using System.Runtime.Intrinsics;
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
    case "MenuData":
        GetMenuData(PID);
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
        MapAssist.Structs.MenuData menudata = processContext.Read<MapAssist.Structs.MenuData>(_MenuDataOffset[pid]);
        
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
    var @class = AnsiConsole.Prompt(
    //     new ConfirmationPrompt("Confirm when you are close to Charsi in Act 1."));
    
    // var previouslyFoundCharsiPtr = IntPtr.Zero;
    // if (AnsiConsole.Prompt(new ConfirmationPrompt("Do you have a previous int pointer offset for Charsi?")))
    // {
    //     previouslyFoundCharsiPtr = IntPtr.Parse(AnsiConsole.Ask<string>("Enter the offset: "));
    // }    
    // var previouslyUnitHashTablePtr = IntPtr.Zero;
    // if (AnsiConsole.Prompt(new ConfirmationPrompt("Do you have a previous int pointer offset the HashTable?")))
    // {
    //     previouslyUnitHashTablePtr = IntPtr.Parse(AnsiConsole.Ask<string>("Enter the offset: "));
    // }
        new SelectionPrompt<string>()
            .AddChoices("Amazon", "Assassin", "Barbarian", "Druid", "Necromancer", "Paladin", "Sorceress")
            .Title("What is your class?")
            .PageSize(8));
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
                GameManager.SetProcessContext(processContext);
                var buffer = processContext.Read<byte>(processContext.BaseAddr, processContext.ModuleSize);
                var monsterTablePointer = IntPtr.Zero;
                var hashTablePointer = IntPtr.Zero;
                var charsiPointer = IntPtr.Zero;
                var shownId = 0;
                var bufferPosition = 0;
                var cancellationTokenSource = new CancellationTokenSource();
                var cancellationTokenSourceForCharsi = new CancellationTokenSource();

                //GetCharsiPtr();
                
                try
                {
                    var startingOffset = (previouslyFoundHashTablePtr == IntPtr.Zero
                    ? 0
                    : (int)((previouslyFoundHashTablePtr.ToInt64() - processContext.BaseAddr.ToInt64())));

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
                                    $"Finding HashTable Offset... {i}/{buffer.Length} --> {Math.Floor((i / (double)buffer.Length * 100))}%");
                            }
                            
                            var currentPointer = IntPtr.Add(processContext.BaseAddr, i);
                            try
                            {
                                var playerHashTable = processContext.Read<MapAssist.Structs.UnitHashTable>(currentPointer);
                                if (playerHashTable.UnitTable[0] != IntPtr.Zero 
                                    && processContext.Read<MapAssist.Structs.UnitAny>(playerHashTable.UnitTable[0]) is UnitAny playerUnit 
                                    && playerUnit.UnitType == UnitType.Player 
                                    && playerUnit.pAct != IntPtr.Zero
                                    && playerUnit.pInventory != IntPtr.Zero
                                    && playerUnit.playerClass.ToString().Equals(@class,StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // var monsterHashTable = processContext.Read<MapAssist.Structs.UnitHashTable>(currentPointer + 128 * 8 * (int)UnitType.Monster);
                                    // if (monsterHashTable.UnitTable[0] != IntPtr.Zero
                                    //     && processContext.Read<MapAssist.Structs.UnitAny>(playerHashTable.UnitTable[0])
                                    //         is UnitAny monsterUnit
                                    //     && monsterUnit.UnitType == UnitType.Monster
                                    //     && monsterUnit.Mode != 0
                                    //     && monsterUnit.Mode != 12
                                    //     && !NPC.Dummies.ContainsKey(monsterUnit.TxtFileNo))
                                    // {
                                    //     var objectHashTable = processContext.Read<MapAssist.Structs.UnitHashTable>(currentPointer + 128 * 8 * (int)UnitType.Object);
                                    //     if (objectHashTable.UnitTable[0] != IntPtr.Zero
                                    //         && processContext.Read<MapAssist.Structs.UnitAny>(
                                    //                 playerHashTable.UnitTable[0])
                                    //             is UnitAny objectUnit
                                    //         && objectUnit.UnitType == UnitType.Object)
                                    //     {
                                    var playerModel = new UnitPlayer(playerHashTable.UnitTable[0]);
                                    // if (playerModel.Name?.Equals("Manaorb", StringComparison.InvariantCultureIgnoreCase) ?? false)
                                    // {
                                        hashTablePointer = currentPointer;
                                        bufferPosition = i;
                                        cancellationTokenSource.Cancel();
                                    // }
                                    // }
                                    // }
                                }
                                
                                
                                
                                // var first = playerHashTable.UnitTable[0];
                                //
                                // if (first != IntPtr.Zero)
                                // {
                                //     var firstUnit = processContext.Read<MapAssist.Structs.UnitAny>(first);
                                //     if (firstUnit.UnitType == UnitType.Monster &&
                                //         playerHashTable.UnitTable.Any(ptr =>
                                //             new UnitMonster(ptr).Npc == Npc.Charsi))
                                //     {
                                //         AnsiConsole.MarkupLine(
                                //             $"Found offset: [bold yellow]{currentPointer.ToInt64() - processContext.BaseAddr.ToInt64():X}[/]");
                                //         monsterTablePointer = currentPointer;
                                //         bufferPosition = i;
                                //         cancellationTokenSource.Cancel();
                                //     }
                                // }
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[bold red]Error[/] - {ex.Message}");
                            }
                            
                            
                        });

                }
                catch (OperationCanceledException ex)
                {
                    AnsiConsole.MarkupLine($"Found HashTable Pointer Offset: [bold yellow]{hashTablePointer}[/]");
                }

                // var playerPointer = IntPtr.Zero;
                // var hashTablePointer = IntPtr.Zero;
                // var cancellationTokenSourceForPlayer = new CancellationTokenSource();
                // var cancellationTokenSourceForHashTable = new CancellationTokenSource();

                // IntPtr GetCharsiPtr()
                // {
                //     try
                //     {
                //         var startingOffset = (previouslyFoundCharsiPtr == IntPtr.Zero
                //             ? 0
                //             : (int)((previouslyFoundCharsiPtr.ToInt64() - processContext.BaseAddr.ToInt64())));
                //         Parallel.For(startingOffset, buffer.Length,
                //             new ParallelOptions
                //             {
                //                 MaxDegreeOfParallelism = 100, CancellationToken = cancellationTokenSourceForCharsi.Token
                //             },
                //             i =>
                //             {
                //                 if (i > shownId)
                //                 {
                //                     shownId = i;
                //                     ctx.Status(
                //                         $"Finding Charsi Pointer Offset... {i}/{buffer.Length} --> {Math.Floor((i / (double)buffer.Length * 100))}%");
                //                 }
                //     
                //                 var currentPointer = IntPtr.Add(processContext.BaseAddr, i);
                //                 try
                //                 {
                //                     //Finding Charsi
                //                     var monsterPtr = processContext.Read<UnitAny>(currentPointer);
                //                     if (monsterPtr.UnitType == UnitType.Monster)
                //                     {
                //                         try
                //                         {
                //                             var monster = new UnitMonster(currentPointer);
                //                             if (monster.Npc == Npc.Charsi)
                //                             {
                //                                 charsiPointer = currentPointer;
                //                                 cancellationTokenSourceForCharsi.Cancel();
                //                             }
                //                         }catch (Exception ex)
                //                         {
                //                             AnsiConsole.MarkupLine($"[bold red]Error[/] - {ex.Message}");
                //                         }
                //                     }
                //                 }
                //                 catch (Exception ex)
                //                 {
                //                     AnsiConsole.MarkupLine($"[bold red]Error[/] - {ex.Message}");
                //                 }
                //             });
                //     }
                //     catch (OperationCanceledException ex)
                //     {
                //         AnsiConsole.MarkupLine($"Found Charsi Pointer Offset: [bold yellow]{charsiPointer}[/]");
                //     }
                //
                //     return charsiPointer;
                // }

                // Finding the HashTable
                // Start by finding the pointer address to the player
                // try
                // {
                //     var startingOffset = (previouslyFoundHashTablePtr == IntPtr.Zero
                //         ? 0
                //         : (int)((previouslyFoundHashTablePtr.ToInt64() - processContext.BaseAddr.ToInt64())));
                //     shownId = 0;
                //     Parallel.For(startingOffset, buffer.Length,
                //         new ParallelOptions
                //         {
                //             MaxDegreeOfParallelism = 100,
                //             CancellationToken = cancellationTokenSourceForHashTable.Token
                //         },
                //         i =>
                //         {
                //             if (i > shownId)
                //             {
                //                 shownId = i;
                //                 ctx.Status(
                //                     $"Finding Unit HashTable Offset... {i}/{buffer.Length} --> {Math.Floor((i / (double)buffer.Length * 100))}%");
                //             }
                //
                //             var currentPointer = IntPtr.Add(processContext.BaseAddr, i);
                //             try
                //             {
                //                 var hashTable = processContext.Read<MapAssist.Structs.UnitHashTable>(currentPointer);
                //                 
                //                 var first = hashTable.UnitTable[0];
                //
                //                 if (first != IntPtr.Zero)
                //                 {
                //                     var firstUnit = processContext.Read<MapAssist.Structs.UnitAny>(first);
                //                     if (firstUnit.UnitType == UnitType.Player && firstUnit.playerClass > 0 &&
                //                         hashTable.UnitTable.Any(ptr =>
                //                             processContext.Read<MapAssist.Structs.UnitAny>(ptr).playerClass.ToString()
                //                                 .Equals(@class, StringComparison.InvariantCultureIgnoreCase)))
                //                     {
                //                         AnsiConsole.MarkupLine(
                //                             $"Found offset: [bold yellow]{currentPointer.ToInt64() - processContext.BaseAddr.ToInt64():X}[/]");
                //                         hashTablePointer = currentPointer;
                //                         bufferPosition = i;
                //                         cancellationTokenSourceForHashTable.Cancel();
                //                     }
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
                //     AnsiConsole.MarkupLine($"Found Unit HashTable Offset: [bold yellow]{hashTablePointer}[/]");
                // }

                AnsiConsole.MarkupLine($"Found buffer position: [bold yellow]{bufferPosition}[/]");
                AnsiConsole.MarkupLine($"Printing buffer area 300 before struct:");
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

void GetMenuData(int pid)
{
    AnsiConsole.Prompt(new ConfirmationPrompt("Do you have 1 - your map showing 2 - skill tree opened?"));
    
    var previousltFoundIntPtr = IntPtr.Zero;
    if (AnsiConsole.Prompt(new ConfirmationPrompt("Do you have a previous int pointer offset?")))
    {
        previousltFoundIntPtr = IntPtr.Parse(AnsiConsole.Ask<string>("Enter the offset: "));
    }

    AnsiConsole.Status()
        .Spinner(Spinner.Known.Star)
        .SpinnerStyle(Style.Parse("green bold"))
        .Start("Finding Menu Data Offset...", ctx =>
        {
            using (var processContext = new ProcessContext(Process.GetProcessById(pid)))
            {
                var buffer = processContext.Read<byte>(processContext.BaseAddr, processContext.ModuleSize);
                var gameNamePointer = IntPtr.Zero;
                var cancellationTokenSource = new CancellationTokenSource();
                var shownId = 0;
                var bufferPosition = 0;
                // try
                // {
                    // "1110010010000xxxxxxxxx0000x00"
                    var pattern = new Pattern("10 00 10 00 1");
                    IntPtr[] patternAddresses = FindAllPatternMatches(processContext.BaseAddr, buffer, pattern);
                    // var offsetBuffer = new byte[4];
                    // var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
                    
                    // AnsiConsole.MarkupLine($"Found Game Name Offset: [bold yellow]{patternAddress}[/]");
                    
                    
                    AnsiConsole.MarkupLine("Please close your skill tree tab");
                    Thread.Sleep(1000);
                    AnsiConsole.MarkupLine("    3");
                    Thread.Sleep(1000);
                    AnsiConsole.MarkupLine("    2");
                    Thread.Sleep(1000);
                    AnsiConsole.MarkupLine("    1");
                    Thread.Sleep(1000);
                    
                    var pattern2 = new Pattern("10 00 00 00 1");
                    buffer = processContext.Read<byte>(processContext.BaseAddr, processContext.ModuleSize);
                    IntPtr[] patternAddresses2 = FindAllPatternMatches(processContext.BaseAddr, buffer, pattern2);

                    // AnsiConsole.MarkupLine($"Found Game Name Offset: [bold yellow]{patternAddress2}[/]");
                    var intersection = patternAddresses.ToList().Intersect(patternAddresses2.ToList());
                    if (intersection.Any())
                    {
                        AnsiConsole.MarkupLine("We found the same address, this is good!");
                        // List of intersections
                        AnsiConsole.MarkupLine($"Found Game Name Offset: [bold yellow]{intersection.First()}[/]");
                        if(intersection.Count() > 1)
                        {
                            AnsiConsole.MarkupLine("We found more than one address, this is bad!");
                        }
                    }
                    
                //     var startingOffset = (previousltFoundIntPtr == IntPtr.Zero
                //         ? 0
                //         : (int)((previousltFoundIntPtr.ToInt64() - processContext.BaseAddr.ToInt64())));
                //     Parallel.For(startingOffset, buffer.Length,
                //         new ParallelOptions
                //         {
                //             MaxDegreeOfParallelism = 100, CancellationToken = cancellationTokenSource.Token
                //         },
                //         i =>
                //         {
                //             if (i > shownId)
                //             {
                //                 shownId = i;
                //                 ctx.Status(
                //                     $"Finding Game Name Offset... {i}/{buffer.Length} --> {Math.Floor((i / (double)buffer.Length * 100))}%");
                //             }
                //
                //             var currentPointer = IntPtr.Add(processContext.BaseAddr, i);
                //             try
                //             {
                //                 var sess = processContext.Read<MapAssist.Structs.Session>(currentPointer);
                //                 if (Encoding.UTF8.GetString(sess.GameName)
                //                     .Equals(gameName, StringComparison.InvariantCultureIgnoreCase))
                //                 {
                //                     gameNamePointer = currentPointer;
                //                     bufferPosition = i;
                //                     cancellationTokenSource.Cancel();
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
                //     AnsiConsole.MarkupLine($"Found Game Name Offset: [bold yellow]{gameNamePointer}[/]");
                // }

                // AnsiConsole.MarkupLine($"Found buffer position: [bold yellow]{bufferPosition}[/]");
                // AnsiConsole.MarkupLine($"Printing buffer area 300 before and 300 into found struct:");
                // var newBytes = new byte[300];
                // Buffer.BlockCopy(buffer, bufferPosition - 300, newBytes, 0, 300);
                // AnsiConsole.MarkupLine("Before:");
                // AnsiConsole.MarkupLine(BitConverter.ToString(newBytes));
                // Buffer.BlockCopy(buffer, bufferPosition, newBytes, 0, 300);
                // AnsiConsole.MarkupLine("After:");
                // AnsiConsole.MarkupLine(BitConverter.ToString(newBytes));
            }
        });
    
    IntPtr FindPattern(IntPtr baseAddr, byte[] buffer, Pattern pattern)
    {
        for (var i = 0; i < buffer.Length; i++)
        {
            if (pattern.Match(buffer, i))
            {
                return IntPtr.Add(baseAddr, i);
            }
        }
        return IntPtr.Zero;
    }
    
    IntPtr[] FindAllPatternMatches(IntPtr baseAddr, byte[] buffer, Pattern pattern)
    {
        List<IntPtr> matches = new();
        for (var i = 0; i < buffer.Length; i++)
        {
            if (pattern.Match(buffer, i))
            {
                matches.Add(IntPtr.Add(baseAddr, i));
            }
        }
        return matches.ToArray();
    }
}

