using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace CatchMeIfYouCan.Scripts.UI
{
    public partial class MainMenu : Control
    {
        public override void _Ready()
        {
            var deployButton = GetNode<TextureButton>("deploy");

            deployButton.Pressed += OnDeployButtonPressed;
        }

        private void OnDeployButtonPressed()
        {
            GD.Print("Deploy button image clicked!");
            // Add your logic to start the game here
        }

        private void OnDifficultyButtonPressed()
        {
            GD.Print("Opening Difficulty Menu...");
            // Logic to show a difficulty sub-menu
        }

        private void OnTerminateButtonPressed()
        {
            GD.Print("System Terminated.");
            // Close the game
            GetTree().Quit();
        }
    }
}