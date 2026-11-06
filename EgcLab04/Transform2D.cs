using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Poziția, rotația și scalarea unui obiect din scenă. Din ele se construiește matricea model,
/// care duce vârfurile obiectului din object space în world space (secțiunea 5).
/// </summary>
public class Transform2D
{
    public Vector2 Position = Vector2.Zero;   // translația, în unități de lume
    public float Rotation = 0.0f;             // unghiul, în GRADE, pozitiv = sens trigonometric
    public Vector2 Scale = Vector2.One;       // factorii de scalare pe x și pe y
    public float Depth = 0.0f;                // coordonata z; contează doar în perspectivă (secțiunea 7.2)

    public Matrix4 ModelMatrix =>
    // T1: ordinea este scalare -> rotatie -> translatie. In OpenTK matricele se inmultesc
    // de la stanga la dreapta, in ordinea in care se aplica varfului.
    // Daca translatia ar fi aplicata inaintea rotatiei, forma ar fi mutata mai intai in Position
    // si apoi rotita in jurul originii lumii, deci ar descrie un arc in jurul originii
    // in loc sa se roteasca pe loc.
    Matrix4.CreateScale(Scale.X, Scale.Y, 1f)
    * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation))
    * Matrix4.CreateTranslation(Position.X, Position.Y, Depth);

    /// <summary>O copie independentă (folosită pentru readucerea scenei în starea inițială).</summary>
    public Transform2D Clone() => (Transform2D)MemberwiseClone();
}
