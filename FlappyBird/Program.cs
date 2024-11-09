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
        {
            Program.Game.MainScene.player.Jump();
        }
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
            float offset = r.Next(-2, 1) - (float)r.NextDouble();
            Wrint.Info(offset);
            Pipe pipeUp = new() { Position = new(4.5f,offset), Scale = new(13,20) };
            Pipe pipeDown = new() { Position = new(4.5f,offset+5), Scale = new(13,20) };
            
            Objects = [pipeUp,pipeDown];
            colliders.Update();
            timer = 0;
        }
    }
}

internal class Player : CollidingSprite
{
    private Gravity gravity = new(0.003f,0.06f);
    private const float jumpStrenght = 0.1f;

    public Player() : base(Program.birdSprite)
    {
        Scale = 13;
    }
    public override void OnCreate(Hierarchy h, GameMaster m)
    {
        base.OnCreate(h, m);
        Position = GetMaster().WindowCenter.ToVec2(GetMaster()) - Vec2.Right;
    }
    public override void OnUpdate(GameMaster m)
    {
        base.OnUpdate(m);
        Position = gravity.Calculate(Position);

        // check if bird is still in borders
        if (Position.Y > 5f || Position.Y < 0f) Die();
        
        if (this.IsColliding(Program.Game.MainScene.colliders,out ICollide? collider))
        {
            if (collider == this) return;
            Die();
        }
    }
    public void Jump()
    {
        gravity.AddForce(jumpStrenght, Vec2.Up);
    }

    private void Die()
    {
        Wrint.Error("You died");
        Application.Exit();
    }
}
internal class Pipe : CollidingSprite
{
    public const float speed = 1.3f;
    public const float spawnDelay = 2.66f;
    public const float destroyAfter = 5;

    private float timer;
    public Pipe() : base(Program.pipeSprite)
    {

    }
    public override void OnUpdate(GameMaster m)
    {
        base.OnUpdate(m);
        Position -= new Vec2(speed, 0) * m.TimeMaster.DeltaTime;
        timer += m.TimeMaster.DeltaTime;
        if (timer > destroyAfter) Destroy(Program.Game.MainScene);
    }
}