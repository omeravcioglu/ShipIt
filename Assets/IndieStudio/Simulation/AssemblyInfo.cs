using System.Runtime.CompilerServices;

// The tests write older save versions to check that they still load (SaveTests).
[assembly: InternalsVisibleTo("IndieStudio.Simulation.Tests")]
