using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3.Control
{
	public abstract class AbstractXp3ControlUiBinding<TOriginator1, TSettings> : AbstractXp3UiBinding<TOriginator1, TSettings>, IXp3ControlUiBinding
		where TOriginator1 : class, IOriginator
		where TSettings : IXp3ControlUiBindingSettings1Originator, new()
	{
		public event EventHandler<GenericEventArgs<IControlCrosspoint>> OnCrosspointChanged;

		private IControlCrosspoint m_Crosspoint;

		public int ControlId { get; private set; }

		public IControlCrosspoint Crosspoint
		{
			get { return m_Crosspoint; }
			protected set
			{
				if (m_Crosspoint == value)
					return;

				Unsubscribe(m_Crosspoint);

				m_Crosspoint = value;

				Subscribe(m_Crosspoint);

				OnCrosspointChanged.Raise(this, m_Crosspoint);
			}
		}


		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ControlId = 0;
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Xp3ControlId = ControlId;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ControlId = settings.Xp3ControlId != null ? settings.Xp3ControlId.Value : 0;

			InstantiateCrosspoint();
		}

		#region Crosspoint Callbacks

		protected virtual void InstantiateCrosspoint()
		{
			CrosspointSystem system = Xp3.GetOrCreateSystem(SystemId);
			Crosspoint = system.GetOrCreateControlCrosspointManager()
								  .CreateCrosspoint(ControlId, Name);
		}

		protected virtual void Subscribe(IControlCrosspoint crosspoint)
		{
		}

		protected virtual void Unsubscribe(IControlCrosspoint crosspoint)
		{
		}

		#endregion
	}

	public abstract class AbstractXp3ControlUiBinding<TOriginator1, TOriginator2, TSettings> : AbstractXp3UiBinding<TOriginator1, TOriginator2, TSettings>, IXp3ControlUiBinding
		where TOriginator1 : class, IOriginator
		where TOriginator2 : class, IOriginator
		where TSettings : IXp3ControlUiBindingSettings2Originators, new()
	{
		public event EventHandler<GenericEventArgs<IControlCrosspoint>> OnCrosspointChanged;

		private IControlCrosspoint m_Crosspoint;

		public int ControlId { get; private set; }

		public IControlCrosspoint Crosspoint
		{
			get { return m_Crosspoint; }
			protected set
			{
				if (m_Crosspoint == value)
					return;

				Unsubscribe(m_Crosspoint);

				m_Crosspoint = value;

				Subscribe(m_Crosspoint);

				OnCrosspointChanged.Raise(this, m_Crosspoint);
			}
		}


		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ControlId = 0;
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Xp3ControlId = ControlId;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ControlId = settings.Xp3ControlId != null ? settings.Xp3ControlId.Value : 0;

			InstantiateCrosspoint();
		}

		#region Crosspoint Callbacks

		protected virtual void InstantiateCrosspoint()
		{
			CrosspointSystem system = Xp3.GetOrCreateSystem(SystemId);
			Crosspoint = system.GetOrCreateControlCrosspointManager()
								  .CreateCrosspoint(ControlId, Name);
		}

		protected virtual void Subscribe(IControlCrosspoint crosspoint)
		{
		}

		protected virtual void Unsubscribe(IControlCrosspoint crosspoint)
		{
		}

		#endregion
	}

	public abstract class AbstractXp3ControlUiBinding<TOriginator1, TOriginator2, TOriginator3, TSettings> : AbstractXp3UiBinding<TOriginator1, TOriginator2, TOriginator3, TSettings>, IXp3ControlUiBinding
		where TOriginator1 : class, IOriginator
		where TOriginator2 : class, IOriginator
		where TOriginator3 : class, IOriginator
		where TSettings : IXp3ControlUiBindingSettings3Originators, new()
	{
		public event EventHandler<GenericEventArgs<IControlCrosspoint>> OnCrosspointChanged;

		private IControlCrosspoint m_Crosspoint;

		public int ControlId { get; private set; }

		public IControlCrosspoint Crosspoint
		{
			get { return m_Crosspoint; }
			protected set
			{
				if (m_Crosspoint == value)
					return;

				Unsubscribe(m_Crosspoint);

				m_Crosspoint = value;

				Subscribe(m_Crosspoint);

				OnCrosspointChanged.Raise(this, m_Crosspoint);
			}
		}


		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ControlId = 0;
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Xp3ControlId = ControlId;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ControlId = settings.Xp3ControlId != null ? settings.Xp3ControlId.Value : 0;

			InstantiateCrosspoint();
		}

		#region Crosspoint Callbacks

		protected virtual void InstantiateCrosspoint()
		{
			CrosspointSystem system = Xp3.GetOrCreateSystem(SystemId);
			Crosspoint = system.GetOrCreateControlCrosspointManager()
								  .CreateCrosspoint(ControlId, Name);
		}

		protected virtual void Subscribe(IControlCrosspoint crosspoint)
		{
		}

		protected virtual void Unsubscribe(IControlCrosspoint crosspoint)
		{
		}

		#endregion
	}

	public abstract class AbstractXp3ControlUiBinding<TOriginator1, TOriginator2, TOriginator3, TOriginator4, TSettings> : AbstractXp3UiBinding<TOriginator1, TOriginator2, TOriginator3, TOriginator4, TSettings>, IXp3ControlUiBinding
		where TOriginator1 : class, IOriginator
		where TOriginator2 : class, IOriginator
		where TOriginator3 : class, IOriginator
		where TOriginator4 : class, IOriginator
		where TSettings : IXp3ControlUiBindingSettings4Originators, new()
	{
		public event EventHandler<GenericEventArgs<IControlCrosspoint>> OnCrosspointChanged;

		private IControlCrosspoint m_Crosspoint;

		public int ControlId { get; private set; }

		public IControlCrosspoint Crosspoint
		{
			get { return m_Crosspoint; }
			protected set
			{
				if (m_Crosspoint == value)
					return;

				Unsubscribe(m_Crosspoint);

				m_Crosspoint = value;

				Subscribe(m_Crosspoint);

				OnCrosspointChanged.Raise(this, m_Crosspoint);
			}
		}


		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			ControlId = 0;
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Xp3ControlId = ControlId;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ControlId = settings.Xp3ControlId != null ? settings.Xp3ControlId.Value : 0;

			InstantiateCrosspoint();
		}

		#region Crosspoint Callbacks

		protected virtual void InstantiateCrosspoint()
		{
			CrosspointSystem system = Xp3.GetOrCreateSystem(SystemId);
			Crosspoint = system.GetOrCreateControlCrosspointManager()
								  .CreateCrosspoint(ControlId, Name);
		}

		protected virtual void Subscribe(IControlCrosspoint crosspoint)
		{
		}

		protected virtual void Unsubscribe(IControlCrosspoint crosspoint)
		{
		}

		#endregion
	}
}