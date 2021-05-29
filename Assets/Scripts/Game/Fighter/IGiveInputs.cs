using UnityEngine;

namespace Game.Fighter
{
    /// <summary>
    /// Gives inputs to the fighter
    /// </summary>
    public interface IGiveInputs
    {

        /// <summary>
        /// obtains the movement input from the left stick
        /// </summary>
        /// <returns>current movement input</returns>
        public Vector2 GetMovementInput();

        /// <summary>
        /// checks to see if the Any button was pressed
        /// </summary>
        /// <returns>true if the player pressed the Any button</returns>
        public bool IsTheAnyButtonPressed();

        /// <summary>
        /// Returns what the current input from the right stick is
        /// </summary>
        /// <returns>right stick action</returns>
        public ActionInput GetRightStickAction();
    }
}