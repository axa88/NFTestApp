using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

using nanoFramework.Json;


namespace NFTestApp
{
	public class Program
	{
		private const string Path = "I:\\ConfigData.json";
		private static ConfigData _configData = new();
		private static IDictionary _data = new Hashtable();

		public static void Main()
		{
			var configRead = ReadConfig();
			var version = new Version(0, 0, 0, 1);
			var equals = Equals((Version)_data[NVElement.Version], version);
			if (!configRead || !equals)
			{
				#if DEBUG
				Debug.WriteLine("NVS Re/Uninitialized");
				#else
				#endif

				const byte mx = byte.MaxValue;
				_data = new Hashtable
				{
					{ NVElement.Version, version },
					{ NVElement.PassKey, new byte[] { 0, 0, 0, 0 } },
					{ NVElement.Ignition, false },
					{ NVElement.IgnitionMode, Mode.Invalid },
					{ NVElement.IgnitionCombo, new[] { mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx, mx } },
					{ NVElement.Indicators, new ConfigurationBase() }
				};

				Thread.Sleep(Timeout.Infinite);
			}
		}

		internal enum NVElement
		{
			Version,
			PassKey,
			Ignition,
			IgnitionMode,
			IgnitionCombo,
			Indicators,
			IndicatorButtonMode,
			IndicatorRate,
			IndicatorOffMode,
			IndicatorOffTime,
			IndicatorOffAngle
		}

		public enum ButtonMode
		{
			Momentary,
			Latched,
			Invalid = byte.MaxValue
		}

		public enum Rate
		{
			Slow,
			Moderate,
			Fast,
			Illegal,
			Count,
			Invalid = byte.MaxValue
		}

		public enum OffMode
		{
			Manual,
			Timer,
			Compass,
			TimerAndCompass = Timer | Compass,
			Count,
			Invalid = byte.MaxValue
		}

		internal enum Mode
		{
			Key,                            // no bt, no controls, ignition on with power.
			Controls,                       // no bt, ignition with control dance, sequence needs to be set
			Bt,                             // ignition on with bt proximity
			BtAndControls = Controls | Bt,  // bt and controls.
			Invalid = byte.MaxValue         // max value for bad read
		}

		private static bool ReadConfig()
		{
			FileStream fileStream = null;

			if (File.Exists(Path))
			{
				try
				{
					fileStream = new(Path, FileMode.Open, FileAccess.Read);
					var buffer = new byte[fileStream.Length];
					if (fileStream.Read(buffer, 0, (int)fileStream.Length) > 0)
					{
						var s = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

						#if DEBUG
						Debug.WriteLine(s);
						#else
						#endif

						var o = JsonConvert.DeserializeObject(s, typeof(ConfigData)); // TODO throws
						_configData = (ConfigData)o;

						_data[NVElement.Version] = _configData.Version;
						_data[NVElement.PassKey] = _configData.PassKey;
						_data[NVElement.Ignition] = _configData.Ignition;
						_data[NVElement.IgnitionMode] = _configData.IgnitionMode;
						_data[NVElement.IgnitionCombo] = _configData.IgnitionCombo;
						_data[NVElement.Indicators] = _configData.IndicatorConfiguration;

						return true;
					}
				}
				catch (Exception e)
				{
					Debug.WriteLine(e.Message);
					return false;
				}
				finally
				{
					fileStream?.Dispose();
				}
			}

			return false;
		}

		public class ConfigurationBase
		{
			// for deserialization
			public ConfigurationBase()
			{
			}

			protected ConfigurationBase(ConfigurationBase @base)
			{
				ButtonMode = @base.ButtonMode;
				Rate = @base.Rate;
				OffMode = @base.OffMode;
				OffDelay = @base.OffDelay;
				OffAngle = @base.OffAngle;
			}

			public ButtonMode ButtonMode { get; set; } = ButtonMode.Invalid;
			public Rate Rate { get; set; } = Rate.Invalid;
			public OffMode OffMode { get; set; } = OffMode.Invalid;
			public byte OffDelay { get; set; }
			public byte OffAngle { get; set; }

			#region Overrides of Object
			public override bool Equals(object obj) => obj is ConfigurationBase cb && ButtonMode == cb.ButtonMode && Rate == cb.Rate && OffMode == cb.OffMode && OffDelay == cb.OffDelay && OffAngle == cb.OffAngle;
			public override int GetHashCode() => (byte)ButtonMode ^ (byte)Rate ^ (byte)OffMode ^ OffDelay ^ OffAngle;
			#endregion
		}

		internal class ConfigData
		{
			public Version Version { get; set; }
			public byte[] PassKey { get; set; }
			public bool Ignition { get; set; }
			public Mode IgnitionMode { get; set; }
			public byte[] IgnitionCombo { get; set; }
			public ConfigurationBase IndicatorConfiguration { get; set; }
		}
	}
}
