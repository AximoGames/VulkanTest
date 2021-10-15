using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Vortice.Win32
{
    /// <summary>
    /// Defines an integer rectangle (Left, Top, Right, Bottom)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DebuggerDisplay("Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}")]
    public struct RawRect
    {
        public RawRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// The left position.
        /// </summary>
        public int Left;

        /// <summary>
        /// The top position.
        /// </summary>
        public int Top;

        /// <summary>
        /// The right position
        /// </summary>
        public int Right;

        /// <summary>
        /// The bottom position.
        /// </summary>
        public int Bottom;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(Left)}: {Left}, {nameof(Top)}: {Top}, {nameof(Right)}: {Right}, {nameof(Bottom)}: {Bottom}";
        }
    }
}