using System;
using System.Windows.Input;

namespace DLab.Infrastructure
{
    [Serializable]
    public class CustomHotKey : HotKey
    {
        private readonly Action _hotkeyAction;

        public CustomHotKey(string name, Key key, ModifierKeys modifiers, bool enabled)
            : base(key, modifiers, enabled)
        {
            Name = name;
        }

        public CustomHotKey(string name, Key key, ModifierKeys modifiers, bool enabled, Action hotkeyAction)
            : base(key, modifiers, enabled)
        {
            _hotkeyAction = hotkeyAction;
            Name = name;
        }

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                if (value != name)
                {
                    name = value;
                    OnPropertyChanged(name);
                }
            }
        }

        protected override void OnHotKeyPress()
        {
            _hotkeyAction();
            base.OnHotKeyPress();
        }


        protected CustomHotKey(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            Name = info.GetString("Name");
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);

            info.AddValue("Name", Name);
        }
    }
}