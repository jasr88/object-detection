/*
 * This code whas generated following the  turotian in
 * https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx?ranMID=24542&ranEAID=je6NUbpObpQ&ranSiteID=je6NUbpObpQ-KmOmjkg1OEDpIAWtcQ0wrA&epi=je6NUbpObpQ-KmOmjkg1OEDpIAWtcQ0wrA&irgwc=1&OCID=AID2000142_aff_7593_1243925&tduid=%28ir__b6vvowqktokfthyjkk0sohzgc32xireljywncfnh00%29%287593%29%281243925%29%28je6NUbpObpQ-KmOmjkg1OEDpIAWtcQ0wrA%29%28%29&irclickid=_b6vvowqktokfthyjkk0sohzgc32xireljywncfnh00
 * with some modificatios to work with unity
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Barracuda;
using UnityEngine;

public class ObjectDetector :MonoBehaviour {

	// 1. First, add the GetAbsolutePath method below the Main method in the Program class.
	// (JARS) This will not be nessesary, we will be assosiating the files directly via Unity editor

	// 2. Then, inside the Main method, create fields to store the location of your assets.
	// (JARS) These are the serialize fields in wich we are gonna toreferenciate the model and the labels file
	[SerializeField]
	private NNModel m_ModelFile = null;
	[SerializeField]
	private TextAsset m_LabelsFile = null;
	[SerializeField]
	private Color[] m_Colors = null;

	private OutputParser outputParser = new OutputParser ();
	private IWorker worker;

	// We are going to skip the read-images-from-the-folder part, because our images are going to be realtime fotograms from a video

	// Settings for the image input
	public struct ImageNetSettings {
		public const int imageHeight = 416;
		public const int imageWidth = 416;
	}

	public struct ModelSettings {
		// for checking Model input and output parameter names,
		// you can use tools like Netron

		public const string ModelInput = "data";
		public const string ModelOutput = "model_outputs0";
	}

	// This detector is designed to read .onnx models (hopefully!!!!)
	private const int IMAGE_MEAN = 0;
	private const float IMAGE_STD = 1f;

	// Minimum detection confidence to consider a detection
	// The bigger the value the major certenty, but less matches found!
	private const float MINIMUM_CONFIDENCE = 0.5f;

	void Start() {
		// Read the file and convert the text inside in a Array separated by lines (Using Linq)
		outputParser.SetLabels (Regex.Split (m_LabelsFile.text, "\n|\r|\r\n").Where (s => !String.IsNullOrEmpty (s)).ToArray ());
		outputParser.SetColors (m_Colors);
		// Load the onnx model file
		var model = ModelLoader.Load (m_ModelFile);
		// Create the barracuda inference engine (breaks down the given model into executable tasks and schedules them on the GPU or CPU)
		this.worker = WorkerFactory.CreateWorker (WorkerFactory.Type.ComputePrecompiled, model);
	}



	public IEnumerator Detect(Color32[] picture, System.Action<IList<BoundingBox>> callback) {
		// Anonymous block thar uses a tenspr param, this tensor isfrom the TransformInput method
		using (var tensor = TransformInput (picture, ImageNetSettings.imageWidth, ImageNetSettings.imageHeight)) {
			var inputs = new Dictionary<string, Tensor> ();
			inputs.Add (ModelSettings.ModelInput, tensor);
			yield return StartCoroutine (worker.StartManualSchedule (inputs));

			var output = worker.PeekOutput (ModelSettings.ModelOutput);
			var results = outputParser.ParseOutputs (output, MINIMUM_CONFIDENCE);
			var boxes = outputParser.FilterBoundingBoxes (results, 5, MINIMUM_CONFIDENCE);

			callback (boxes);
		}
	}

	// Transform picture to tensor without the WinML library
	public static Tensor TransformInput(Color32[] pic, int width, int height) {
		float[] floatValues = new float[width * height * 3];

		for (int i = 0; i < pic.Length; ++i) {
			var color = pic[i];

			floatValues[i * 3 + 0] = (color.r - IMAGE_MEAN) / IMAGE_STD;
			floatValues[i * 3 + 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
			floatValues[i * 3 + 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
		}

		return new Tensor (1, height, width, 3, floatValues);
	}
}
