﻿using WFGL.Core;
using WFGL.Input;
using WFGL.Objects;
using WFGL.Physics;
using WFGL.Rendering;
using WFGL.Utilities;
using WFGL.Components;
using WFGL.UI;

namespace FlappyBird;

internal static class Assets
{
    public static readonly Bitmap birdSprite = new("Bird.png");
    public static readonly Bitmap pipeSprite = new("Pipe.png");
    public static readonly Bitmap background = new("Background.png");
    public static readonly Font mainFont = new(StringRenderer.DEFALUT_FONT_NAME, 22);
}

internal class Program
{
    #pragma warning disable CS8618 
    public static Game Game { get; private set; }

    #pragma warning restore CS8618

    private static void Main(string[] args)
    {
        GameWindow win = new(GameWindowOptions.Default with { Title = "FlappyBird", Size = new(800, 800)});
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
        GameWindow.RegisterInput(new GameInput());
        MainScene = new(this);
        RegisterHierarchy(MainScene);
    }
}

internal class GameInput : InputHandler
{
    protected override void OnKeyDown(Keys key)
    {
        base.OnKeyDown(key);
        Program.Game.MainScene.player.Jump();
    }
    protected override void OnMouseDown(MouseButtons buttons)
    {
        base.OnMouseDown(buttons);
        Program.Game.MainScene.player.Jump();
    }
}
internal class MainScene : Hierarchy
{
    public readonly Player player;
    public readonly BitmapRenderer background;
    public readonly Group<ICollide> colliders;

    public MainScene(GameMaster m) : base(m) 
    { 
        player = new();
        background = new(Assets.background);
        colliders = new(this);

        Init = [
            background,
            player
        ];
        colliders.Update();

        background.Scale = Master.VirtualSize.ToVec2(Master.VirtualScale) * 2;
        new Counter(m.TimeMaster, Pipe.spawnDelay, true, SpawnPipes);
    }

    // TODO: Add object pooling
    public void SpawnPipes()
    {
        var r = new Random();
        float offset = -(float)r.NextDouble() - 1f;

        Pipe pipeUp = new() { Position = new(4.5f, offset), Scale = new(13, 20) };
        Pipe pipeDown = new() { Position = new(4.5f, offset + 5), Scale = new(13, 20) };

        Init = [pipeUp, pipeDown];
        colliders.Update();
    }
}

internal class Player : GravityTransform
{
    private const float jumpStrenght = 175;
    private CollidingBitmapRenderer bitmap;
    public Player()
    {
        bitmap = new(Assets.birdSprite);
        bitmap.Scale = 13;
    }
    public override void OnCreate(Hierarchy h, GameMaster m)
    {
        base.OnCreate(h, m);
        bitmap.SetMaster(m);
        Position = Master.Center.ToVec2(Master.VirtualScale) - Vec2.Right;
    }
    public override void OnUpdate()
    {
        base.OnUpdate();

        bitmap.Position = Position;
        bitmap.OnUpdate();

        // check if bird is still in our view borders
        if (Position.Y > 5f || 
            Position.Y < 0f || 
            bitmap.IsColliding(Program.Game.MainScene.colliders, out ICollide? collider)) Die();
    }
    public override void OnDraw()
    {
        base.OnDraw();
        bitmap.OnDraw();
        bitmap.Draw(Master, Master.Renderer);
    }
    public void Jump()
    {
        ResetVelocity();
        AddForce(new(jumpStrenght * Master.TimeMaster.DeltaTimeF, Vec2.Up));
    }

    private void Die()
    {
        Program.Game.MainScene.Init = 
            [new StringRenderer(Assets.mainFont, "YOU DIED", Color.Red) 
            { Position = Master.Center.ToVec2(Master.VirtualScale) }];

        Wrint.Error("You died");
        Master.TimeMaster.Stop();
    }
}
internal class Pipe : CollidingBitmapRenderer
{
    public static float speed = 1.5f;
    public static float spawnDelay = 2.7f;
    public static float destroyAfter = 5;

    public Pipe() : base(Assets.pipeSprite) { }
    public override void OnCreate(Hierarchy h, GameMaster m)
    {
        base.OnCreate(h, m);
        new Counter(m.TimeMaster, destroyAfter, false, End);
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        Position -= new Vec2(speed, 0) * Master.TimeMaster.DeltaTimeF;
    }
    private void End() => Destroy(Program.Game.MainScene);
}