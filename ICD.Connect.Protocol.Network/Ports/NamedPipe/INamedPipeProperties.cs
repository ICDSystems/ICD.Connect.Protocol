#if !SIMPLSHARP
using System;
using System.IO.Pipes;
using System.Security.Principal;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Protocol.Network.Ports.NamedPipe
{
	public interface INamedPipeProperties
	{
		/// <summary>
		/// Gets/sets the configurable remote hostname.
		/// </summary>
		[IpAddressSettingsProperty]
		string NamedPipeHostname { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe name.
		/// </summary>
		string NamedPipeName { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe direction.
		/// </summary>
		PipeDirection? NamedPipeDirection { get; set; }

		/// <summary>
		/// Gets/sets the configurable pipe options.
		/// </summary>
		PipeOptions? NamedPipeOptions { get; set; }

		/// <summary>
		/// Gets/sets the configurable token impersonation level.
		/// </summary>
		TokenImpersonationLevel? NamedPipeTokenImpersonationLevel { get; set; }

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void ClearNamedPipeProperties();
	}

	public static class NamedPipePropertiesExtensions
	{
		/// <summary>
		/// Copies the configured properties from the given NamedPipe Properties instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		public static void Copy(this INamedPipeProperties extends, INamedPipeProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			extends.NamedPipeHostname = other.NamedPipeHostname;
			extends.NamedPipeName = other.NamedPipeName;
			extends.NamedPipeDirection = other.NamedPipeDirection;
			extends.NamedPipeOptions = other.NamedPipeOptions;
			extends.NamedPipeTokenImpersonationLevel = other.NamedPipeTokenImpersonationLevel;
		}

		/// <summary>
		/// Updates the NamedPipe Properties instance where values are not already configured.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="hostname"></param>
		/// <param name="name"></param>
		/// <param name="direction"></param>
		/// <param name="options"></param>
		/// <param name="tokenImpersonationLevel"></param>
		public static void ApplyDefaultValues(this INamedPipeProperties extends, string hostname, string name,
		                                      PipeDirection? direction, PipeOptions? options,
		                                      TokenImpersonationLevel? tokenImpersonationLevel)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (extends.NamedPipeHostname == null)
				extends.NamedPipeHostname = hostname;

			if (extends.NamedPipeName == null)
				extends.NamedPipeName = name;

			if (extends.NamedPipeDirection == null)
				extends.NamedPipeDirection = direction;

			if (extends.NamedPipeOptions == null)
				extends.NamedPipeOptions = options;

			if (extends.NamedPipeTokenImpersonationLevel == null)
				extends.NamedPipeTokenImpersonationLevel = tokenImpersonationLevel;
		}

		/// <summary>
		/// Creates a new properties instance, applying this instance over the top of the other instance.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static INamedPipeProperties Superimpose(this INamedPipeProperties extends, INamedPipeProperties other)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (other == null)
				throw new ArgumentNullException("other");

			NamedPipeProperties output = new NamedPipeProperties();

			output.Copy(extends);
			output.ApplyDefaultValues(other.NamedPipeHostname, other.NamedPipeName, other.NamedPipeDirection,
			                          other.NamedPipeOptions, other.NamedPipeTokenImpersonationLevel);

			return output;
		}
	}
}
#endif
