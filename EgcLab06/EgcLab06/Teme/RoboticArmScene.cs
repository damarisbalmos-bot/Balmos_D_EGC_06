// Varianta A: brațul robotic
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class RoboticArmScene : Scene
{
    private const string BaseLeft = "braț: baza la stânga";
    private const string BaseRight = "braț: baza la dreapta";
    private const string ShoulderUp = "braț: umăr în sus";
    private const string ShoulderDown = "braț: umăr în jos";
    private const string ElbowUp = "braț: cot în sus";
    private const string ElbowDown = "braț: cot în jos";
    private const string Grip = "braț: deschide / închide cleștele";
    private const string SavePose = "braț: salvează poza (cadru-cheie)";
    private const string Play = "braț: redă pozele în buclă / oprește";
    private const string NextEase = "braț: easing-ul redării";
    private const string ClearPoses = "braț: șterge pozele";

    private const float TurnSpeed = 70.0f;
    private const float ShoulderMin = -90.0f, ShoulderMax = 90.0f;
    private const float ElbowMin = -150.0f, ElbowMax = 150.0f;
    private const float UpperLength = 2.5f, ForeLength = 2.0f;
    private const float GripOpen = 0.6f, GripClosed = 0.2f;

    private SceneObject _yaw = null!, _shoulder = null!, _elbow = null!;
    private SceneObject _fingerL = null!, _fingerR = null!;

    private Vector3 _pose = new(0.0f, 20.0f, -40.0f);

    private readonly Tween _grip = new(new Vector3(GripOpen, 0, 0), new Vector3(GripOpen, 0, 0), 0.1f, Easing.EaseOut);
    private bool _gripOpen = true;

    private readonly KeyframeTrack _track = new(1.0f);
    private bool _playing;
    private float _playTime;
    private int _easeIndex;

    public override string Name => "braț robotic";

    public override string Status =>
        $"poze: {_track.Count} | {(_playing ? $"redare ({Easing.All[_easeIndex].Name})" : "manual")} | clește {(_gripOpen ? "deschis" : "închis")}";

    public override IEnumerable<string> ActionNames => new[]
    {
        BaseLeft, BaseRight, ShoulderUp, ShoulderDown, ElbowUp, ElbowDown, Grip, SavePose, Play, NextEase, ClearPoses,
    };

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(6.0f, 5.0f, 9.0f);
        CameraTarget = new Vector3(0.0f, 2.2f, 0.0f);

        input.Bind(BaseLeft, Keys.A, Keys.Left);
        input.Bind(BaseRight, Keys.D, Keys.Right);
        input.Bind(ShoulderUp, Keys.W);
        input.Bind(ShoulderDown, Keys.S);
        input.Bind(ElbowUp, Keys.Up);
        input.Bind(ElbowDown, Keys.Down);
        input.Bind(Grip, Keys.Space);
        input.Bind(SavePose, Keys.K);
        input.Bind(Play, Keys.Enter, Keys.KeyPadEnter);
        input.Bind(NextEase, Keys.E);
        input.Bind(ClearPoses, Keys.Backspace);

        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 24, 12));

        AddObject("podea", grid);

        SceneObject pedestal = AddObject("soclu", cube);
        pedestal.Transform.Position = new Vector3(0.0f, 0.3f, 0.0f);
        pedestal.Transform.Scale = new Vector3(1.8f, 0.6f, 1.8f);
        pedestal.Tint = new Color4(0.35f, 0.38f, 0.45f, 1.0f);

        _yaw = AddObject("baza rotativă", null);
        _yaw.Transform.Position = new Vector3(0.0f, 0.6f, 0.0f);

        SceneObject turret = AddObject("turelă", cube, _yaw);
        turret.Transform.Position = new Vector3(0.0f, 0.2f, 0.0f);
        turret.Transform.Scale = new Vector3(1.0f, 0.4f, 1.0f);
        turret.Tint = new Color4(0.9f, 0.5f, 0.2f, 1.0f);

        _shoulder = AddObject("umăr", null, _yaw);
        _shoulder.Transform.Position = new Vector3(0.0f, 0.4f, 0.0f);
        AddJoint(sphere, _shoulder, 0.35f);
        AddSegment(cube, "braț superior", _shoulder, UpperLength, 0.4f, new Color4(0.95f, 0.65f, 0.2f, 1.0f));

        _elbow = AddObject("cot", null, _shoulder);
        _elbow.Transform.Position = new Vector3(0.0f, UpperLength, 0.0f);
        AddJoint(sphere, _elbow, 0.3f);
        AddSegment(cube, "antebraț", _elbow, ForeLength, 0.32f, new Color4(0.95f, 0.8f, 0.3f, 1.0f));

        SceneObject wrist = AddObject("încheietură", null, _elbow);
        wrist.Transform.Position = new Vector3(0.0f, ForeLength, 0.0f);
        AddJoint(sphere, wrist, 0.24f);

        _fingerL = AddObject("deget stânga", cube, wrist);
        _fingerR = AddObject("deget dreapta", cube, wrist);
        foreach (SceneObject f in new[] { _fingerL, _fingerR })
        {
            f.Transform.Scale = new Vector3(0.15f, 0.8f, 0.3f);
            f.Tint = new Color4(0.8f, 0.85f, 0.9f, 1.0f);
        }

        ApplyPose();
        ApplyGrip();
    }

    private void AddJoint(Mesh sphere, SceneObject pivot, float radius)
    {
        SceneObject j = AddObject($"articulație {pivot.Name}", sphere, pivot);
        j.Transform.Scale = new Vector3(radius * 2.0f);
        j.Tint = new Color4(0.25f, 0.3f, 0.4f, 1.0f);
    }

    private void AddSegment(Mesh cube, string name, SceneObject pivot, float length, float width, Color4 color)
    {
        SceneObject s = AddObject(name, cube, pivot);
        s.Transform.Position = new Vector3(0.0f, length / 2.0f, 0.0f);
        s.Transform.Scale = new Vector3(width, length, width);
        s.Tint = color;
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        if (input.WasPressed(Grip))
        {
            _gripOpen = !_gripOpen;
            _grip.GoTo(new Vector3(_gripOpen ? GripOpen : GripClosed, 0, 0), 0.35f);
            _grip.Ease = Easing.EaseOut;
        }
        _grip.Update(dt);

        if (input.WasPressed(SavePose))
        {
            _track.Add(_pose);
            ConsolePanel.PrintMessage($"poza {_track.Count} salvată: baza {_pose.X:F0}°, umăr {_pose.Y:F0}°, cot {_pose.Z:F0}°");
        }
        if (input.WasPressed(ClearPoses))
        {
            _track.Clear();
            _playing = false;
            ConsolePanel.PrintMessage("pozele au fost șterse");
        }
        if (input.WasPressed(NextEase))
        {
            _easeIndex = (_easeIndex + 1) % Easing.All.Length;
            _track.Ease = Easing.All[_easeIndex].Function;
            ConsolePanel.PrintMessage($"easing redare: {Easing.All[_easeIndex].Name}");
        }
        if (input.WasPressed(Play))
        {
            if (_playing) _playing = false;
            else if (_track.Count >= 2) { _playing = true; _playTime = 0.0f; }
            else ConsolePanel.PrintMessage("salvează cel puțin 2 poze (K) înainte de redare");
        }

        if (_playing)
        {
            _playTime += dt;
            _pose = _track.Sample(_playTime);
        }
        else
        {
            _pose.X += input.Axis(BaseLeft, BaseRight) * TurnSpeed * dt;
            _pose.Y = Math.Clamp(_pose.Y + input.Axis(ShoulderDown, ShoulderUp) * TurnSpeed * dt, ShoulderMin, ShoulderMax);
            _pose.Z = Math.Clamp(_pose.Z + input.Axis(ElbowDown, ElbowUp) * TurnSpeed * dt, ElbowMin, ElbowMax);
        }

        ApplyPose();
        ApplyGrip();
    }

    private void ApplyPose()
    {
        _yaw.Transform.Rotation = new Vector3(0.0f, _pose.X, 0.0f);
        _shoulder.Transform.Rotation = new Vector3(0.0f, 0.0f, _pose.Y);
        _elbow.Transform.Rotation = new Vector3(0.0f, 0.0f, _pose.Z);
    }

    private void ApplyGrip()
    {
        float gap = _grip.Value.X;
        _fingerL.Transform.Position = new Vector3(-gap, 0.4f, 0.0f);
        _fingerR.Transform.Position = new Vector3(gap, 0.4f, 0.0f);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Poza curentă   : baza {_pose.X:F1}°  umăr {_pose.Y:F1}°  cot {_pose.Z:F1}°";
        yield return $"Clește         : {(_gripOpen ? "deschis" : "închis")}  deschidere {_grip.Value.X * 2.0f:F2}";
        yield return $"Poze salvate   : {_track.Count}   redare: {(_playing ? $"da, t = {_playTime % _track.Duration:F2} / {_track.Duration:F1} s" : "nu")}   " +
                     $"easing: {Easing.All[_easeIndex].Name}";
    }
}