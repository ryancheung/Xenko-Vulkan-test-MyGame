using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace MyGame
{
    public static class DXManager
    {
        public static Game1 Game { get; private set; }

        public static PresentationParameters Parameters { get; private set; }
        public static GraphicsDevice Device { get { return Game.GraphicsDevice; } }
        public static GraphicsContext GraphicsContext { get { return Game.GraphicsContext; } }
        public static SpriteBatch Sprite { get; private set; }
        public static SpriteBatch Line { get { return Sprite; } }

        static MutablePipelineState _pipelineState;
        public static MutablePipelineState PipelineState
        {
            get
            {
                if (_pipelineState == null)
                {
                    _pipelineState = new MutablePipelineState(Device);
                }

                return _pipelineState;
            }
        }

        public static Texture CurrentRenderTarget { get; private set; } = null;

        public static float Opacity { get; private set; } = 1F;

        public static bool Blending { get; private set; }
        public static float BlendRate { get; private set; } = 1F;

        public static bool DeviceLost { get; set; }
        
        public static Texture ScratchTexture;

        private static byte[] _PalleteData = null;

        public static Texture PoisonTexture;

        static DXManager()
        {
        }

        public static void Create(Game1 gameMir)
        {
            Game = gameMir;

#if ANDROID || IOS
            Config.FullScreen = true;
#endif

            Parameters = new PresentationParameters();
            PipelineState.State.BlendState = BlendStates.AlphaBlend;
        }

        public static unsafe void LoadTextures()
        {
            // CleanUp();

            if (Sprite == null)
                Sprite = new SpriteBatch(Device);

            PoisonTexture = Texture.New2D(Device, 6, 6, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Default);

            var ret = new int[6*6];
            for (int y = 0; y < 6; y++)
                for (int x = 0; x < 6; x++)
                    if (x == 0 || y == 0 || x == 5 || y == 5)
                        ret[y * 6 + x] = -16777216;
                    else
                        ret[y * 6 + x] = -1;
            PoisonTexture.SetData<int>(GraphicsContext.CommandList, ret);

            ScratchTexture = Texture.New2D(Device, Parameters.BackBufferWidth, Parameters.BackBufferHeight, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 1, GraphicsResourceUsage.Default);
        }

        private static void CleanUp()
        {
            if (Sprite != null)
            {
                if (!Sprite.IsDisposed)
                    Sprite.Dispose();

                Sprite = null;
            }

            if (ScratchTexture != null)
            {
                if (!ScratchTexture.IsDisposed)
                    ScratchTexture.Dispose();

                ScratchTexture = null;
            }

            if (PoisonTexture != null)
            {
                if (!PoisonTexture.IsDisposed)
                    PoisonTexture.Dispose();

                PoisonTexture = null;
            }
        }

        public static void MemoryClear()
        {
        }

        public static void Unload()
        {
            CleanUp();

            if (Device != null)
            {
                if (!Device.IsDisposed)
                    Device.Dispose();
            }
        }

        public static String GetMetrics()
        {
            return "DXManager.GetMetrics is not implemented.";
        }

        public static void SetRenderTarget(Texture renderTarget = null)
        {
            if (CurrentRenderTarget == renderTarget) return;

            Sprite.End();
            Sprite.Begin(GraphicsContext, SpriteSortMode.Deferred, PipelineState.State.BlendState);

            CurrentRenderTarget = renderTarget;

            if (renderTarget == null)
                GraphicsContext.CommandList.SetRenderTarget(Device.Presenter.DepthStencilBuffer, Device.Presenter.BackBuffer);
            else
                GraphicsContext.CommandList.SetRenderTarget(Device.Presenter.DepthStencilBuffer, renderTarget);
        }

        static SamplerState _samplerSateForTransform = null;

        private static Matrix _customTransform;
        public static Matrix SpriteTransform
        {
            set
            {
                if (_samplerSateForTransform == null)
                {
                    var desc = SamplerStateDescription.Default;
                    desc.Filter = TextureFilter.Anisotropic;
                    desc.MaxAnisotropy = 2;
                    _samplerSateForTransform = SamplerState.New(Device, desc);
                }

                DXManager.Sprite.End();

                _customTransform = value;

                if (_customTransform == Matrix.Identity)
                    Sprite.Begin(GraphicsContext, SpriteSortMode.Deferred, PipelineState.State.BlendState);
                else
                {
                    PipelineState.State.BlendState.AlphaToCoverageEnable = false;
                    Sprite.Begin(GraphicsContext, _customTransform, SpriteSortMode.Deferred, PipelineState.State.BlendState, _samplerSateForTransform);
                }
            }
        }

        public static void UpdatePipelineState()
        {
            //PipelineState.Update();

            //GraphicsContext.CommandList.SetPipelineState(PipelineState.CurrentState);
        }

        public static void SetOpacity(float opacity)
        {
            if (Opacity == opacity)
                return;

            Sprite.End();

            if (opacity >= 1 || opacity < 0)
            {
                PipelineState.State.BlendState.AlphaToCoverageEnable = false;
                PipelineState.State.BlendState.RenderTarget0.ColorSourceBlend = Blend.SourceAlpha;
                PipelineState.State.BlendState.RenderTarget0.ColorDestinationBlend = Blend.InverseSourceAlpha;
                PipelineState.State.BlendState.RenderTarget0.AlphaSourceBlend = Blend.One;
                GraphicsContext.CommandList.SetBlendFactor(Color4.White);
            }
            else
            {
                PipelineState.State.BlendState.AlphaToCoverageEnable = true;
                PipelineState.State.BlendState.RenderTarget0.ColorSourceBlend = Blend.BlendFactor;
                PipelineState.State.BlendState.RenderTarget0.ColorDestinationBlend = Blend.InverseBlendFactor;
                PipelineState.State.BlendState.RenderTarget0.AlphaSourceBlend = Blend.SourceAlpha;
                GraphicsContext.CommandList.SetBlendFactor(new Color4(opacity));
            }
            UpdatePipelineState();

            Sprite.Begin(GraphicsContext, SpriteSortMode.Deferred, PipelineState.State.BlendState);
            
            Opacity = opacity;
        }
        public static void SetBlend(bool value, float rate = 1F, Blend? colorSourceBlendOverride = null)
        {
            if (value == Blending) return;

            Blending = value;
            BlendRate = 1F;

            Sprite.End();

            if (Blending)
            {
                PipelineState.State.BlendState.AlphaToCoverageEnable = true;
                PipelineState.State.BlendState.RenderTarget0.ColorSourceBlend = colorSourceBlendOverride ?? Blend.BlendFactor;
                PipelineState.State.BlendState.RenderTarget0.ColorDestinationBlend = Blend.One;
                GraphicsContext.CommandList.SetBlendFactor(new Color4(rate));
                UpdatePipelineState();

                Sprite.Begin(GraphicsContext, SpriteSortMode.Deferred, PipelineState.State.BlendState);
            }
            else
            {
                PipelineState.State.BlendState.RenderTarget0.ColorSourceBlend = Blend.One;
                PipelineState.State.BlendState.RenderTarget0.ColorDestinationBlend = Blend.InverseSourceAlpha;

                GraphicsContext.CommandList.SetBlendFactor(Color4.White);
                UpdatePipelineState();

                Sprite.Begin(GraphicsContext, SpriteSortMode.Deferred, PipelineState.State.BlendState);
            }

            if (CurrentRenderTarget == null)
                GraphicsContext.CommandList.SetRenderTarget(Device.Presenter.DepthStencilBuffer, Device.Presenter.BackBuffer);
            else
                GraphicsContext.CommandList.SetRenderTarget(Device.Presenter.DepthStencilBuffer, CurrentRenderTarget);
        }

        public static void ResetDevice()
        {
            CleanUp();

            DeviceLost = true;

            if (CEnvir.Target.ClientSize.Width == 0 || CEnvir.Target.ClientSize.Height == 0) return;

            Parameters.BackBufferWidth = CEnvir.Target.ClientSize.Width;
            Parameters.BackBufferWidth = CEnvir.Target.ClientSize.Height;

            // Change back buffer size on Game.BeginRun() would cause rendering issue.
            Game.GraphicsDeviceManager.PreferredBackBufferWidth = CEnvir.Target.ClientSize.Width;
            Game.GraphicsDeviceManager.PreferredBackBufferHeight = CEnvir.Target.ClientSize.Height;

            Game.GraphicsDeviceManager.IsFullScreen = false;
            Game.GraphicsDeviceManager.PreferredColorSpace = ColorSpace.Gamma;
            Game.GraphicsDeviceManager.PreferredMultisampleCount = MultisampleCount.X2;
            Game.GraphicsDeviceManager.PreferredBackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
            Game.GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D16_UNorm;

            Device.Presenter.PresentInterval = false ? PresentInterval.Default : PresentInterval.Immediate;

            Game.GraphicsDeviceManager.ApplyChanges();

            LoadTextures();
        }
        public static void AttemptReset()
        {
                ResetDevice();
                DeviceLost = false;
        }
        public static void AttemptRecovery()
        {
            Sprite.End();

            GraphicsContext.CommandList.SetRenderTarget(Device.Presenter.DepthStencilBuffer, Device.Presenter.BackBuffer);
        }
        
        public static void ToggleFullScreen()
        {
            if (CEnvir.Target == null) return;

#if ANDROID || IOS
            return;
#endif

            ResetDevice();
        }

        public static void SetResolution(System.Drawing.Size size)
        {
#if ANDROID || IOS
            size.Width = Device.DisplayMode.Width;
            size.Height = Device.DisplayMode.Height;
#endif
            if (CEnvir.Target.ClientSize == size) return;

            GraphicsContext.CommandList.Clear(Device.Presenter.BackBuffer, Color4.Black);
            GraphicsContext.CommandList.Clear(Device.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

            CEnvir.Target.ClientSize = size;

            ResetDevice();
        }
    }

    internal class CEnvir
    {
        public static TargetForm Target { get; internal set; } = new TargetForm();
    }

    public class TargetForm
    {
        public System.Drawing.Size ClientSize { get; set; } = new System.Drawing.Size(800, 600);
        public Rectangle DisplayRectangle {  get { return new Rectangle(0, 0, ClientSize.Width, ClientSize.Height); } }
        public void Close() { }
    }
}
