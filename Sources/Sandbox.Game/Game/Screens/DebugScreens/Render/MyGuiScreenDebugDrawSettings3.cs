﻿using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using VRage;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Screens.DebugScreens
{
    [MyDebugScreen("Render", "Debug draw settings 3")]
    class MyGuiScreenDebugDrawSettings3 : MyGuiScreenDebugBase
    {
        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugDrawSettings3";
        }

        public MyGuiScreenDebugDrawSettings3()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);
            m_currentPosition.Y += 0.01f;
            m_scale = 0.7f;

            AddCaption("Debug draw settings 3", Color.Yellow.ToVector4());
            AddShareFocusHint();

            AddCheckBox("Decals", MyRenderProxy.Settings, MemberHelper.GetMember(() => MyRenderProxy.Settings.DebugDrawDecals));
        }
    }
}
