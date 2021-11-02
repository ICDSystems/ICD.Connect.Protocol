using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Protocol.Crosspoints.Crosspoints;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Protocol.Crosspoints.UiBindings.XP3.Equipment
{
	public abstract class AbstractXp3EquipmentUiBinding<TOriginator1, TSettings> : AbstractXp3UiBinding<TOriginator1, TSettings>, IXp3EquipmentUiBinding
		where TOriginator1 : class, IOriginator
		where TSettings : IXp3EquipmentUiBindingSettings1Originator, new()
	{
		public event EventHandler<GenericEventArgs<IEquipmentCrosspoint>> OnCrosspointChanged;

		private IEquipmentCrosspoint m_Crosspoint;

		public int EquipmentId { get; private set; }

		public IEquipmentCrosspoint Crosspoint
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

			EquipmentId = 0;
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Xp3EquipmentId = EquipmentId;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);
           
			EquipmentId = settings.Xp3EquipmentId != null ? settings.Xp3EquipmentId.Value : 0;

			InstantiateCrosspoint();
		}

		#region Crosspoint Callbacks

		protected virtual void InstantiateCrosspoint()
		{
			CrosspointSystem system = Xp3.GetOrCreateSystem(SystemId);
			Crosspoint = system.GetOrCreateEquipmentCrosspointManager()
								  .CreateCrosspoint(EquipmentId, Name);
		}

		protected virtual void Subscribe(IEquipmentCrosspoint crosspoint)
		{
		}

		protected virtual void Unsubscribe(IEquipmentCrosspoint crosspoint)
		{
		}

		#endregion
	}

	public abstract class AbstractXp3EquipmentUiBinding<TOriginator1, TOriginator2, TSettings> : AbstractXp3UiBinding<TOriginator1, TOriginator2, TSettings>, IXp3EquipmentUiBinding
		where TOriginator1 : class, IOriginator
		where TOriginator2 : class, IOriginator
		where TSettings : IXp3EquipmentUiBindingSettings2Originators, new()
	{
		public event EventHandler<GenericEventArgs<IEquipmentCrosspoint>> OnCrosspointChanged;

		private IEquipmentCrosspoint m_Crosspoint;

		public int EquipmentId { get; private set; }

		public IEquipmentCrosspoint Crosspoint
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

			EquipmentId = 0;
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Xp3EquipmentId = EquipmentId;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			EquipmentId = settings.Xp3EquipmentId != null ? settings.Xp3EquipmentId.Value : 0;

			InstantiateCrosspoint();
		}

		#region Crosspoint Callbacks

		protected virtual void InstantiateCrosspoint()
		{
			CrosspointSystem system = Xp3.GetOrCreateSystem(SystemId);
			Crosspoint = system.GetOrCreateEquipmentCrosspointManager()
								  .CreateCrosspoint(EquipmentId, Name);
		}

		protected virtual void Subscribe(IEquipmentCrosspoint crosspoint)
		{
		}

		protected virtual void Unsubscribe(IEquipmentCrosspoint crosspoint)
		{
		}

		#endregion
	}

	public abstract class AbstractXp3EquipmentUiBinding<TOriginator1, TOriginator2, TOriginator3, TSettings> : AbstractXp3UiBinding<TOriginator1, TOriginator2, TOriginator3, TSettings>, IXp3EquipmentUiBinding
		where TOriginator1 : class, IOriginator
		where TOriginator2 : class, IOriginator
		where TOriginator3 : class, IOriginator
		where TSettings : IXp3EquipmentUiBindingSettings3Originators, new()
	{
		public event EventHandler<GenericEventArgs<IEquipmentCrosspoint>> OnCrosspointChanged;

		private IEquipmentCrosspoint m_Crosspoint;

		public int EquipmentId { get; private set; }

		public IEquipmentCrosspoint Crosspoint
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

			EquipmentId = 0;
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Xp3EquipmentId = EquipmentId;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			EquipmentId = settings.Xp3EquipmentId != null ? settings.Xp3EquipmentId.Value : 0;

			InstantiateCrosspoint();
		}

		#region Crosspoint Callbacks

		protected virtual void InstantiateCrosspoint()
		{
			CrosspointSystem system = Xp3.GetOrCreateSystem(SystemId);
			Crosspoint = system.GetOrCreateEquipmentCrosspointManager()
								  .CreateCrosspoint(EquipmentId, Name);
		}

		protected virtual void Subscribe(IEquipmentCrosspoint crosspoint)
		{
		}

		protected virtual void Unsubscribe(IEquipmentCrosspoint crosspoint)
		{
		}

		#endregion
	}

	public abstract class AbstractXp3EquipmentUiBinding<TOriginator1, TOriginator2, TOriginator3, TOriginator4, TSettings> : AbstractXp3UiBinding<TOriginator1, TOriginator2, TOriginator3, TOriginator4, TSettings>, IXp3EquipmentUiBinding
		where TOriginator1 : class, IOriginator
		where TOriginator2 : class, IOriginator
		where TOriginator3 : class, IOriginator
		where TOriginator4 : class, IOriginator
		where TSettings : IXp3EquipmentUiBindingSettings4Originators, new()
	{
		public event EventHandler<GenericEventArgs<IEquipmentCrosspoint>> OnCrosspointChanged;

		private IEquipmentCrosspoint m_Crosspoint;

		public int EquipmentId { get; private set; }

		public IEquipmentCrosspoint Crosspoint
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

			EquipmentId = 0;
		}

		protected override void CopySettingsFinal(TSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Xp3EquipmentId = EquipmentId;
		}

		protected override void ApplySettingsFinal(TSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			EquipmentId = settings.Xp3EquipmentId != null ? settings.Xp3EquipmentId.Value : 0;

			InstantiateCrosspoint();
		}

		#region Crosspoint Callbacks

		protected virtual void InstantiateCrosspoint()
		{
			CrosspointSystem system = Xp3.GetOrCreateSystem(SystemId);
			Crosspoint = system.GetOrCreateEquipmentCrosspointManager()
								  .CreateCrosspoint(EquipmentId, Name);
		}

		protected virtual void Subscribe(IEquipmentCrosspoint crosspoint)
		{
		}

		protected virtual void Unsubscribe(IEquipmentCrosspoint crosspoint)
		{
		}

		#endregion
	}
}