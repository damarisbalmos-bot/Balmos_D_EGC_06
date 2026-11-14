using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class OrbitCamera : Camera
{
    public Vector3 Target { get; private set; }
    public float Distance { get; private set; }
    public float Azimuth { get; private set; }
    public float Elevation { get; private set; }

    public const float MaxElevation = 89.0f;
    public const float MinDistance = 2.0f;
    public const float MaxDistance = 40.0f;

    public float MouseSensitivity = 0.3f;
    public float KeyRotateSpeed = 90.0f;
    public float ZoomFactor = 0.9f;
    public float TargetMoveSpeed = 3.0f;

    private static readonly Vector3 WorldUp = Vector3.UnitY;
    private static readonly Vector3 StartTarget = new(0.0f, 1.0f, -2.0f);
    private const float StartDistance = 9.0f;
    private const float StartAzimuth = 90.0f;
    private const float StartElevation = 20.0f;

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "orbită";

    public override Vector3 Position
    {
        get
        {
            float az = MathHelper.DegreesToRadians(Azimuth);
            float el = MathHelper.DegreesToRadians(Elevation);
            return Target + Distance * new Vector3(
                MathF.Cos(el) * MathF.Cos(az),
                MathF.Sin(el),
                MathF.Cos(el) * MathF.Sin(az));
        }
    }

    public override Vector3 Front => Vector3.Normalize(Target - Position);

    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, WorldUp);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation += mouseDelta.Y * MouseSensitivity;
        }

        float step = KeyRotateSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth += step;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth -= step;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation += step;
        if (keyboard.IsKeyDown(Keys.Down)) Elevation -= step;

        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);
        Azimuth %= 360.0f;

        if (mouse.ScrollDelta.Y != 0.0f)
        {
            Distance *= MathF.Pow(ZoomFactor, mouse.ScrollDelta.Y);
            Distance = Math.Clamp(Distance, MinDistance, MaxDistance);
        }

        // BONUS: W A S D mută ținta în plan orizontal
        Vector3 forward = Front;
        forward.Y = 0.0f;
        if (forward.LengthSquared > 1e-8f)
        {
            forward.Normalize();
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, WorldUp));

            Vector3 move = Vector3.Zero;
            if (keyboard.IsKeyDown(Keys.W)) move += forward;
            if (keyboard.IsKeyDown(Keys.S)) move -= forward;
            if (keyboard.IsKeyDown(Keys.D)) move += right;
            if (keyboard.IsKeyDown(Keys.A)) move -= right;

            if (move.LengthSquared > 1e-8f)
            {
                move.Normalize();
                Target += move * TargetMoveSpeed * dt;
            }
        }
    }

    // BONUS: comutare fără salt FPS -> orbită
    public void LookFrom(Vector3 eye, Vector3 front, float distance)
    {
        front.Normalize();
        Vector3 o = -front;
        Distance = Math.Clamp(distance, MinDistance, MaxDistance);
        Target = eye + front * Distance;
        Elevation = Math.Clamp(MathHelper.RadiansToDegrees(MathF.Asin(o.Y)), -MaxElevation, MaxElevation);
        Azimuth = MathHelper.RadiansToDegrees(MathF.Atan2(o.Z, o.X));
    }

    public override void Reset()
    {
        Target = StartTarget;
        Distance = StartDistance;
        Azimuth = StartAzimuth;
        Elevation = StartElevation;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 p = Position;
        yield return $"Țintă          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanță       : {Distance,6:F2}";
        yield return $"Azimut / elev. : {Azimuth,7:F1}° / {Elevation,6:F1}°";
        yield return $"Poziție        : ({p.X,6:F2}, {p.Y,6:F2}, {p.Z,6:F2})";
    }
}
