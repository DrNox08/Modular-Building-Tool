using UnityEngine;
using System.Collections.Generic;

namespace BuildingToolUtils
{
    public class EditorKeyToggle
    {
        private readonly KeyCode key;
        private readonly EventModifiers requiredModifiers;

        //Centralization
        private static readonly Dictionary<(KeyCode, EventModifiers), EditorKeyToggle> toggles = new();

        public static bool Ctrl(KeyCode key) => Check(key, EventModifiers.Control);
        public static bool Alt(KeyCode key) => Check(key, EventModifiers.Alt);
        public static bool Shift(KeyCode key) => Check(key, EventModifiers.Shift);
        public static bool SpaceBar() => Check(KeyCode.Space, EventModifiers.None);


        private bool isHeld;
        private bool consumed;

        public EditorKeyToggle(KeyCode key, EventModifiers modifiers = EventModifiers.None)
        {
            this.key = key;
            this.requiredModifiers = modifiers;
        }
        private static bool Check(KeyCode key, EventModifiers mods)
        {
            Event e = Event.current;
            if (e == null || !e.isKey) return false;

            var id = (key, mods);
            if (!toggles.ContainsKey(id))
                toggles[id] = new EditorKeyToggle(key, mods);

            return toggles[id].UpdateAndCheck(e);
        }

        public void Update(Event e)
        {
            if (e == null || !e.isKey || e.keyCode != key)
                return;

            if (requiredModifiers != EventModifiers.None && (e.modifiers & requiredModifiers) != requiredModifiers)
                return;

            if (e.type == EventType.KeyDown)
            {
                if (!isHeld && !consumed)
                {
                    isHeld = true;
                    consumed = true;
                }
            }
            else if (e.type == EventType.KeyUp)
            {
                isHeld = false;
                consumed = false;
            }
        }
        
        public static bool CtrlMiddleMouseClick()
        {
            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown && e.button == 2)
            {
               
                bool ctrlKeyHeld = Event.current.shift;

                if (ctrlKeyHeld)
                {
                    e.Use();
                    return true;
                }
            }
            return false;
        }

        public bool Pressed()
        {
            if (consumed && isHeld)
            {
                consumed = false;
                return true;
            }
            return false;
        }

        public bool UpdateAndCheck(Event e)
        {
            Update(e);
            return Pressed();
        }

    }
}





