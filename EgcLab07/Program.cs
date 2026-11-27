//damaris.balmos@student.usv.ro
using Egc;

// Program.cs conține DOAR inițializarea aplicației:
// creăm fereastra (clasa Game) și pornim bucla principală.
try
{
    using var game = new Game(1024, 640, "EGC – Laborator 07");
    game.Run();
}
catch (Exception ex)
{
    File.WriteAllText(@"C:\Users\Public\eroare.txt", ex.ToString());
    throw;
}
