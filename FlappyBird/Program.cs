using WFGL.Core;
using WFGL.Input;
using WFGL.Objects;
using WFGL.Physics;
using WFGL.Rendering;
using WFGL.Utilities;

namespace FlappyBird;

internal class Program
{
    #pragma warning disable CS8618 
    public static Game Game { get; private set; }
    #pragma warning restore CS8618

    public static readonly Bitmap birdSprite = new("Bird.png");
    public static readonly Bitmap pipeSprite = new("Pipe.png");
    public static readonly Bitmap background = new("Background.png");
    private static void Main(string[] args)
    {
        
        GameWindow win = new(GameWindowOptions.Default with { Title = "FlappyBird", Size = new(800,800)});
        Game = new(win);
        Game.Load();
    }
}
internal class Game : GameMaster
{
    public MainScene MainScene { get; private set; }
    public Game(GameWindow window) : base(window)
    {
        WindowAspectLock = true;
        RegisterInput(new GameInput(this));
        MainScene = new(this);
        RegisterHierarchy(MainScene);
    }
}

internal class GameInput(GameMaster m) : InputHandler(m)
{
    protected override void OnMouseDown(MouseButtons buttons)
    {
        base.OnMouseDown(buttons);
        if(buttons == MouseButtons.Left)
            Program.Game.MainScene.player.Jump();
    }
}
internal class MainScene : Hierarchy
{
    public readonly Player player;
    public readonly BitmapRenderer background;
    public readonly Group<ICollide> colliders;

    private float timer;

    public MainScene(GameMaster m) : base(m) 
    { 
        player = new();
        background = new(Program.background);
        colliders = new(this);

        Objects = [
            background,
            player
        ];
        colliders.Update();
    }

    public override void OnUpdate(GameMaster m)
    {
        base.OnUpdate(m);
        background.Scale = m.RenderSize.ToVec2(m) * 2;

        timer += m.TimeMaster.DeltaTime;
        if(timer > Pipe.spawnDelay)
        {
            var r = new Random();
            float offset = -(float)r.NextDouble() -1f;

            Pipe pipeUp = new() { Position = new(4.5f, offset), Scale = new(13, 20) };
            Pipe pipeDown = new() { Position = new(4.5f, offset + 5), Scale = new(13, 20) };

            Objects = [pipeUp, pipeDown];
            colliders.Update();
            timer = 0;
        }
    }
}

internal class Player : GravityTransform
{
    private const float jumpStrenght = 3f;
    private CollidingBitmap bitmap;
    public Player()
    {
        bitmap = new(Program.birdSprite);
        MaxVelocity = 0.2f;
        bitmap.Scale = 13;
    }
    public override void OnCreate(Hierarchy h, GameMaster m)
    {
        base.OnCreate(h, m);
        bitmap.SetMaster(m);
        Position = GetMaster().WindowCenter.ToVec2(GetMaster()) - Vec2.Right;
    }
    public override void OnUpdate(GameMaster m)
    {
        base.OnUpdate(m);

        bitmap.Position = Position;
        bitmap.OnUpdate(m);
        // check if bird is still in borders
        if (Position.Y > 5f || Position.Y < 0f) Die();
        
        if (bitmap.IsColliding(Program.Game.MainScene.colliders,out ICollide? collider))
        {

            Die();
        }
    }
    public override void OnDraw(GameMaster m)
    {
        base.OnDraw(m);
        bitmap.OnDraw(m);
        bitmap.Draw(m,m.Renderer);
    }
    public void Jump()
    {
        ResetVelocity();
        AddForce(new(jumpStrenght, Vec2.Up));
    }

    private void Die()
    {
        Wrint.Error("You died");
        GetMaster().TimeMaster.Stop();
    }
}
internal class Pipe : CollidingBitmap
{
    public const float speed = 1.3f;
    public const float spawnDelay = 2.66f;
    public const float destroyAfter = 5;

    private float timer;
    public Pipe() : base(Program.pipeSprite) { }
    public override void OnUpdate(GameMaster m)
    {
        base.OnUpdate(m);
        Position -= new Vec2(speed, 0) * m.TimeMaster.DeltaTime;
        timer += m.TimeMaster.DeltaTime;
        if (timer > destroyAfter) Destroy(Program.Game.MainScene);
    }
}
