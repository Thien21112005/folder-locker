using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace FolderLocker.Views.Controls
{
    /// <summary>
    /// Vùng kéo thả có thể focus bằng Tab và kích hoạt bằng Enter/Space.
    /// Các label con chuyển tiếp click và sự kiện kéo thả lên panel, nên thả ở đâu trong vùng cũng nhận.
    /// </summary>
    internal sealed class DropZonePanel : Guna2Panel
    {
        private bool _dragOver;
        private Color _normalBorder = Color.Gray;
        private Color _activeBorder = Color.Blue;
        private Color _normalFill = Color.White;
        private Color _activeFill = Color.AliceBlue;

        public event EventHandler? Activated;

        public DropZonePanel()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            AllowDrop = true;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.PushButton;
        }

        public void SetColors(Color normalBorder, Color activeBorder, Color normalFill, Color activeFill)
        {
            _normalBorder = normalBorder;
            _activeBorder = activeBorder;
            _normalFill = normalFill;
            _activeFill = activeFill;
            UpdateVisual();
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsDragTarget
        {
            get => _dragOver;
            set { _dragOver = value; UpdateVisual(); }
        }

        private void UpdateVisual()
        {
            bool active = _dragOver || Focused;
            BorderColor = active ? _activeBorder : _normalBorder;
            FillColor = _dragOver ? _activeFill : _normalFill;
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); UpdateVisual(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); UpdateVisual(); }

        protected override bool IsInputKey(Keys keyData) =>
            keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Activated?.Invoke(this, EventArgs.Empty);
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnClick(EventArgs e)
        {
            Focus();
            base.OnClick(e);
            Activated?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            var child = e.Control!;
            child.AllowDrop = true;
            child.Cursor = Cursors.Hand;
            child.Click += (_, _) => OnClick(EventArgs.Empty);
            child.DragEnter += (_, args) => OnDragEnter(args);
            child.DragOver += (_, args) => OnDragOver(args);
            child.DragLeave += (_, args) => OnDragLeave(args);
            child.DragDrop += (_, args) => OnDragDrop(args);
        }
    }
}
