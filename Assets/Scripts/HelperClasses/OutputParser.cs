using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;
class CellDimensions :DimensionsBase { }

public class OutputParser {
	// Number of classes to detect (Tags in the model) 
	public int classCount = 1;

	// Constant Values for the ONNX model
	public const int ROW_COUNT = 13;
	public const int COL_COUNT = 13;
	public const int CHANNEL_COUNT = 125;
	public const int BOXES_PER_CELL = 5;
	public const int BOX_INFO_FEATURE_COUNT = 5;
	public const float CELL_WIDTH = 32;
	public const float CELL_HEIGHT = 32;

	// Anchors are pre-defined height and width ratios of bounding boxes.
	private float[] anchors = new float[] {
		1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F
	};
	// The length of this array must match with the ClassCount variable
	private string[] labels = null;
	// There are colors associated with each of the classes.
	private static Color[] colors = null;

	public void SetClassCount(int count) {
		classCount = count;
	}

	public void SetLabels(string[] classLabels) {
		labels = classLabels;
	}

	public void SetColors(Color[] classColors) {
		colors = classColors;
	}

	// Applies the sigmoid function that outputs a number between 0 and 1.
	private float Sigmoid(float value) {
		var k = (float)Math.Exp (value);
		return k / (1.0f + k);
	}

	// Normalizes an input vector into a probability distribution.
	private float[] Softmax(float[] values) {
		var maxVal = values.Max ();
		var exp = values.Select (v => Math.Exp (v - maxVal));
		var sumExp = exp.Sum ();

		return exp.Select (v => (float)(v / sumExp)).ToArray ();
	}

	// Skip the "GetOffset" method due the Tensor class in Unity's barracuda supports a multi-dimension array
	// Extracts the bounding box dimensions from the model output.
	private BoundingBoxDimensions ExtractBoundingBoxDimensions(Tensor modelOutput, int x, int y, int channel) {
		return new BoundingBoxDimensions {
			X = modelOutput[0, x, y, channel],
			Y = modelOutput[0, x, y, channel + 1],
			Width = modelOutput[0, x, y, channel + 2],
			Height = modelOutput[0, x, y, channel + 3]
		};
	}

	// Extracts the confidence value that states how sure the model is that it has detected an object and uses the Sigmoid function to turn it into a percentage.
	private float GetConfidence(Tensor modelOutput, int x, int y, int channel) {
		return Sigmoid (modelOutput[0, x, y, channel + 4]);
	}

	// Uses the bounding box dimensions and maps them onto its respective cell within the image.
	private CellDimensions MapBoundingBoxToCell(int x, int y, int box, BoundingBoxDimensions boxDimensions) {
		return new CellDimensions {
			X = ((float)y + Sigmoid (boxDimensions.X)) * CELL_WIDTH,
			Y = ((float)x + Sigmoid (boxDimensions.Y)) * CELL_HEIGHT,
			Width = (float)Math.Exp (boxDimensions.Width) * CELL_WIDTH * anchors[box * 2],
			Height = (float)Math.Exp (boxDimensions.Height) * CELL_HEIGHT * anchors[box * 2 + 1],
		};
	}

	// Extracts the class predictions for the bounding box from the model output and turns them into a probability distribution using the Softmax method.
	public float[] ExtractClasses(Tensor modelOutput, int x, int y, int channel) {
		float[] predictedClasses = new float[classCount];
		int predictedClassOffset = channel + BOX_INFO_FEATURE_COUNT;

		for (int predictedClass = 0; predictedClass < classCount; predictedClass++) {
			predictedClasses[predictedClass] = modelOutput[0, x, y, predictedClass + predictedClassOffset];
		}

		return Softmax (predictedClasses);
	}

	// Selects the class from the list of predicted classes with the highest probability.
	private ValueTuple<int, float> GetTopResult(float[] predictedClasses) {
		return predictedClasses
			.Select ((predictedClass, index) => (Index: index, Value: predictedClass))
			.OrderByDescending (result => result.Value)
			.First ();
	}

	// Filters overlapping bounding boxes with lower probabilities.
	private float IntersectionOverUnion(Rect boundingBoxA, Rect boundingBoxB) {
		var areaA = boundingBoxA.width * boundingBoxA.height;

		if (areaA <= 0)
			return 0;

		var areaB = boundingBoxB.width * boundingBoxB.height;

		if (areaB <= 0)
			return 0;

		var minX = Math.Max (boundingBoxA.xMin, boundingBoxB.xMin);
		var minY = Math.Max (boundingBoxA.yMin, boundingBoxB.yMin);
		var maxX = Math.Min (boundingBoxA.xMax, boundingBoxB.xMax);
		var maxY = Math.Min (boundingBoxA.yMax, boundingBoxB.yMax);

		var intersectionArea = Math.Max (maxY - minY, 0) * Math.Max (maxX - minX, 0);

		return intersectionArea / (areaA + areaB - intersectionArea);
	}

	public IList<BoundingBox> ParseOutputs(Tensor modelTensorOutput, float threshold = 0.3F) {
		List<BoundingBox> boxes = new List<BoundingBox> ();

		for (int row = 0; row < COL_COUNT; row++) {
			for (int colum = 0; colum < ROW_COUNT; colum++) {
				for (int box = 0; box < BOXES_PER_CELL; box++) {

					int channel = (box * (classCount + BOX_INFO_FEATURE_COUNT));
					BoundingBoxDimensions bbd = ExtractBoundingBoxDimensions (modelTensorOutput, colum, row, channel);
					float confidence = GetConfidence (modelTensorOutput, colum, row, channel);

					CellDimensions mappedBoundingBox = MapBoundingBoxToCell (colum, row, box, bbd);

					if (confidence < threshold) {
						continue;
					}

					float[] predictedClasses = ExtractClasses (modelTensorOutput, colum, row, channel);
					var (topResultIndex, topResultScore) = GetTopResult (predictedClasses);
					var topScore = topResultScore * confidence;

					if (topScore < threshold) {
						continue;
					}


					boxes.Add (new BoundingBox {
						Dimensions = new BoundingBoxDimensions {
							X = (mappedBoundingBox.X - mappedBoundingBox.Width / 2),
							Y = (mappedBoundingBox.Y - mappedBoundingBox.Height / 2),
							Width = mappedBoundingBox.Width,
							Height = mappedBoundingBox.Height,
						},
						Confidence = topScore,
						Label = labels[topResultIndex],
						BoxColor = colors[topResultIndex]
					});
				}
			}
		}
		return boxes;
	}

	public IList<BoundingBox> FilterBoundingBoxes(IList<BoundingBox> boxes, int limit, float threshold) {
		var activeCount = boxes.Count;
		var isActiveBoxes = new bool[boxes.Count];

		for (int i = 0; i < isActiveBoxes.Length; i++) {
			isActiveBoxes[i] = true;
		}

		var sortedBoxes = boxes.Select ((b, i) => new { Box = b, Index = i })
				.OrderByDescending (b => b.Box.Confidence)
				.ToList ();

		var results = new List<BoundingBox> ();

		for (int i = 0; i < boxes.Count; i++) {
			if (isActiveBoxes[i]) {
				var boxA = sortedBoxes[i].Box;
				results.Add (boxA);

				if (results.Count >= limit)
					break;

				for (var j = i + 1; j < boxes.Count; j++) {
					if (isActiveBoxes[j]) {
						var boxB = sortedBoxes[j].Box;

						if (IntersectionOverUnion (boxA.Rect, boxB.Rect) > threshold) {
							isActiveBoxes[j] = false;
							activeCount--;

							if (activeCount <= 0)
								break;
						}
					}
				}

				if (activeCount <= 0)
					break;
			}
		}
		return results;
	}
}
