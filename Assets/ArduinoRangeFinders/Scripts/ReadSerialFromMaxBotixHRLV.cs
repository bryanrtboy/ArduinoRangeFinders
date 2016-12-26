//Bryan Leister - June 2016
//
//This script reads from the serial port on it's own thread and passes to the byteReciever (still in the same thread)
//NOTE:  You cannot run Unity operations off the main thread, and so you must be careful to pass data in such a way
//that is thread safe. I am approaching this by setting booleans in both the Reader and Reciever script, when true
//do the Unity stuff and making sure to not try to do 'Unity stuff' with data that is owned by the other thread.
//
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.IO.Ports;
using UnityEngine.UI;

namespace ArduinoSerialReader
{
	public class ReadSerialFromMaxBotixHRLV : MonoBehaviour
	{
		public Dropdown m_dropDownList;
		//For Mac, my serial port name is /dev/cu.usbserial-AM01PK2W
		//For PC, my serial port is either COM3 or COM4
		public string m_name = "COM3";
		public int m_baudRate = 9600;
		public Text m_debugMessages;
		public float m_refreshRate = .1f;
		//Performance is greatly impacted by the refresh rate and timeout, lower refresh rate is faster polling of the arduino,
		//this means the timeout is also called more often. A refresh of .1f with a timeout of 10 seems to be most responsive.
		public int m_timeout = 10;
		//How long after a poll do we wait for a response?
	

		bool isStartingToRead = false;
		string m_storedPortName = "SerialPortName";
		List<string> m_comPortsFound = new List<string> ();

		SerialPort sp;
		//convert this to a float if that is all the Arduino is sending
		string rightNum = "0";
		string leftNum = "0";
		bool isPollingLeft = true;


		void Awake ()
		{
			if (PlayerPrefs.HasKey (m_storedPortName))
				m_name = PlayerPrefs.GetString (m_storedPortName);

			if (m_dropDownList != null) {
				m_dropDownList.ClearOptions ();

				#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
				Debug.Log ("Found " + GetPortNames ().Length + " serial ports");
				foreach (string s in GetPortNames()) { //Local method for getting Mac Port Names
					m_comPortsFound.Add (s);
				}
				#else
				foreach (string s in SerialPort.GetPortNames()) {
					m_comPortsFound.Add (s);
				}
				#endif

				m_comPortsFound.Add ("None"); //Put a default entry so we can 'change' ports
				m_dropDownList.AddOptions (m_comPortsFound);

				//Select the port if we have saved it previously in Player Preference
				for (int i = 0; i < m_dropDownList.options.Count; i++) {
					if (m_dropDownList.options [i].text == m_name)
						m_dropDownList.value = i;
				}

				//m_dropDownList.RefreshShownValue ();

			}
		}

		void Start ()
		{
			#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

			string str = "Found " + GetPortNames ().Length + " serial ports";
//			Debug.Log (str);

			if (m_debugMessages)
				m_debugMessages.text = str;

			foreach (string s in GetPortNames()) {
				Debug.Log (s);
				if (m_debugMessages)
					m_debugMessages.text += "\n" + s;
			}
			#else

			string str = "Found " + SerialPort.GetPortNames ().Length + " serial ports";
//			Debug.Log (str);

			if (m_debugMessages)
				m_debugMessages.text = str;

			foreach (string s in SerialPort.GetPortNames()) {
//				Debug.Log (s);
				if (m_debugMessages)
					m_debugMessages.text += "\n" + s;
			}
			#endif


			if (SerialPortNameExists ()) {
				OpenSerialPort ();
				InvokeRepeating ("PollArduino", 0, m_refreshRate);
			} else {
				Debug.LogError ("No Serial port named " + m_name);

				if (m_debugMessages)
					m_debugMessages.text = "No Serial port named " + m_name;
			}
		}

		void UpdateMessages (string s)
		{
			if (m_debugMessages != null)
				m_debugMessages.text = s;

		}

		#region Set up and Open the Serial Port

		public void ChangeSerialPortName (int value)
		{
			m_name = m_comPortsFound [value];
			PlayerPrefs.SetString (m_storedPortName, m_name);

			if (SerialPortNameExists ()) {
				if (sp == null) {
					OpenSerialPort ();
				} else {
					if (sp.IsOpen) {
						Debug.Log ("Serial port already exists and is open!");
						if (m_debugMessages)
							m_debugMessages.text = "Serial port already exists and is open!";
					} else {
						Debug.Log ("Serial port already exists, but it's not open! Trying to open it now...");

						if (m_debugMessages)
							m_debugMessages.text = "Serial port already exists, but it's not open! Trying to open it now...";

						OpenSerialPort ();
					}
				}
			}
		}

		bool SerialPortNameExists ()
		{
			bool noMatches = true;
			#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
			foreach (string s in GetPortNames()) {
				if (s == m_name)
					noMatches = false;  //At least one name matches
				Debug.Log (s);
			}

			#else
			foreach (string s in SerialPort.GetPortNames()) {
				if (s == m_name)
					noMatches = false;  //At least one name matches
				//Debug.Log (s);
			}
			#endif

			return !noMatches; //Return true
		}

		void OpenSerialPort ()
		{
			sp = new SerialPort (m_name, m_baudRate);
			sp.ReadTimeout = m_timeout;
			sp.Open ();
		}

		void OnDisable ()
		{
			if (sp != null && sp.IsOpen)
				sp.Close ();
		}

		string[] GetPortNames ()
		{
			int p = (int)Environment.OSVersion.Platform;
			List<string> serial_ports = new List<string> ();
         
			// Are we on Unix?
			if (p == 4 || p == 128 || p == 6) {
				string[] ttys = Directory.GetFiles ("/dev/", "*");

				foreach (string dev in ttys) {
					if (dev.StartsWith ("/dev/tty.") || dev.StartsWith ("/dev/cu.")) {
						serial_ports.Add (dev);	
						//	Debug.Log (String.Format (dev));
					}
				}
			}
			return serial_ports.ToArray ();
		}

		#endregion

		#region Reading and Writing to the Arduino

		void PollArduino ()
		{
			string message = "L";

			if (!isPollingLeft) //Alternate between reading from the Left or Right PW sensor in order to provide clean input
				message = "R";

			WriteToArduino (message); 
			ReadIncomingMessages ();

			isPollingLeft = !isPollingLeft;
//			Debug.Log (isPollingLeft + " " + Time.time);
		}

		public void WriteToArduino (string message)
		{
			if (sp != null && sp.IsOpen) {
				sp.WriteLine (message);
				sp.BaseStream.Flush ();
			} else {
				Debug.LogError ("Serial port is not open or does not exist");
			}
		}

		void ReadIncomingMessages ()
		{
			if (sp.IsOpen && Time.time > 2) {
				isStartingToRead = true; //Only read if we are not already doing this...
				try {
					string msg = "";
					string numberRead = sp.ReadLine ();
					if (numberRead != null) {
						if (isPollingLeft)
							leftNum = numberRead;
						else
							rightNum = numberRead;

						UpdateMessages ("Left : " + leftNum.ToString () + " :: Right : " + rightNum.ToString ());
						isStartingToRead = false; //Read was successful, start a new read next interval
					}
				} catch (System.Exception) {
					isStartingToRead = false;  //Read failed, start a new read at next interval
					Debug.LogWarning ("Exception occured at " + Time.time);
					throw;
				}
			}
		}

		#endregion



	}


}