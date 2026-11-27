using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Tema 7, varianta A: ceasul. Tot desenul se face în clock.frag, pe un singur Quad transparent.
/// C# calculează doar valorile (ora, minutul, secundele) din timpul de JOC (dt), deci pauza
/// și scara timpului (P, [ ], N) merg. Space = tema de zi / noapte (Tween pe uniforma uNight).
/// </summary>
public sealed class ClockScene : Scene
{
    private const string ToggleTheme = "ceas: zi / noapte";

    private Material _clock = null!;
    private Tween _night = null!;
    private bool _isNight;
    private double _seconds;   // secunde de la miezul nopții, în timp de joc

    public override string Name => "ceas";

    public override IEnumerable<string> ActionNames => new[] { ToggleTheme };

    public override string Status =>
        $"{(int)(_seconds / 3600) % 24:00}:{(int)(_seconds / 60) % 60:00}:{(int)_seconds % 60:00} | {(_isNight ? "noapte" : "zi")}";

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 0.0f, 5.0f);
        CameraTarget = Vector3.Zero;

        input.Bind(ToggleTheme, Keys.Space);

        Mesh quad = AddMesh(Shapes.Quad(2.0f, 2.0f));

        // Parametri reglabili cu săgețile: 2 float + 1 int.
        _clock = AddMaterial("ceas", "procedural.vert", "clock.frag")
            .Param("uHandWidth", 0.5f, 2.5f, 1.0f)
            .Param("uRimWidth", 0.02f, 0.12f, 0.06f)
            .Param("uTickStep", 1, 5, 1);
        _clock.Transparent = true;   // în afara cadranului alfa = 0

        _clock.Uniforms.Set("uHour", 0.0f);
        _clock.Uniforms.Set("uMinute", 0.0f);
        _clock.Uniforms.Set("uSecond", 0.0f);
        _clock.Uniforms.Set("uNight", 0.0f);
        _clock.Uniforms.Set("uFaceDay", new Vector3(0.97f, 0.96f, 0.92f));
        _clock.Uniforms.Set("uFaceNight", new Vector3(0.08f, 0.10f, 0.18f));

        _night = Tween.Float(0.0f, 0.0f, 0.8f, Easing.EaseInOut);

        AddObject("ceas", quad, _clock).Transform.Scale = new Vector3(1.8f);

        // ora de start = ora reală a calculatorului; apoi curge cu timpul de joc
        _seconds = DateTime.Now.TimeOfDay.TotalSeconds;
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        _seconds = (_seconds + dt) % 86400.0;

        if (input.WasPressed(ToggleTheme))
        {
            _isNight = !_isNight;
            _night.GoTo(_isNight ? 1.0f : 0.0f, 0.8f);
            ConsolePanel.PrintMessage(_isNight ? "tema: noapte" : "tema: zi");
        }
        _night.Update(dt);

        // Secundarul "sare" o dată pe secundă: 0,15 s de EaseOut spre secunda următoare.
        double whole = Math.Floor(_seconds % 60.0);
        float frac = (float)(_seconds % 1.0);
        float jump = Easing.EaseOut(Math.Clamp(frac / 0.15f, 0.0f, 1.0f));
        float second = (float)whole + jump;

        _clock.Uniforms.Set("uSecond", second);
        _clock.Uniforms.Set("uMinute", (float)(_seconds / 60.0 % 60.0));
        _clock.Uniforms.Set("uHour", (float)(_seconds / 3600.0 % 12.0));
        _clock.Uniforms.Set("uNight", _night.Scalar);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Ora            : {Status}";
    }
}