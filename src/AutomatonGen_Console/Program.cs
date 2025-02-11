using AutomataVideoGenerator.Automatons.Redundant.Standard;
using AutomataVideoGenerator.Automatons.Redundant.GPUCompute;
using AutomataVideoGenerator.Automatons.GPUCompute;

new GenericBSN(100, 50, GenericBSN.defaults.GameOfLife).run(50, "GOLResults", 10);
new GenericBSN(100, 100, GenericBSN.defaults.Maze).run(50, "MazeResults", 10);
new GenericBSN(100, 100, GenericBSN.defaults.Bugs).run(50, "BugsResults", 10);
new GenericBSN(1000, 1000, GenericBSN.defaults.DayAndNight).run(50, "DayAndNightResulty");

