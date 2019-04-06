using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Games;

namespace MyGame
{
    public class Game1 : Game
    {
        protected override void BeginRun()
        {
            base.BeginRun();

            Setup();
        }

        void Setup()
        {
            DXManager.Create(this);
            DXManager.ResetDevice();
        }

        protected override Task LoadContent()
        {
            IsFixedTimeStep = false;

            return base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            DXManager.GraphicsContext.CommandList.Clear(DXManager.Device.Presenter.BackBuffer, Color.CornflowerBlue);
            DXManager.Sprite.Begin(DXManager.GraphicsContext);
            DXManager.Sprite.Draw(DXManager.PoisonTexture, new Vector2(0));
            DXManager.Sprite.End();

            base.Draw(gameTime);
        }
    }
}
