using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;

namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3.Control
{
	public interface IXp3ControlUiBinding : IXp3UiBinding
	{
		event EventHandler<GenericEventArgs<IControlCrosspoint>> OnCrosspointChanged;

		IControlCrosspoint Crosspoint { get; }
	}
}