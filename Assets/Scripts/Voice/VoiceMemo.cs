//* Script for voice memo during geosampling

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Windows.Speech; // For Windows speech recognition
using System.Linq;
using System.Collections.Generic;

public class VoiceMemoRecorder : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private GameObject voiceMemoPanel; // empty gameobject to store voice memo components
    [SerializeField] private TextMeshProUGUI transcriptionText; // test of transcribed audio
    [SerializeField] private Button doneButton; 
    [SerializeField] private Image microphoneIcon; // static icon
    [SerializeField] private GameObject waveformVisualizer;
}