using Xenko.Engine;
using Xenko.Graphics;

namespace MyGame.Windows
{
    class MyGameApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game1())
            {
                game.Run();
            }
        }
    }
}
