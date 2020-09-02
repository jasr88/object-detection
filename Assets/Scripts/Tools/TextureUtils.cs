using System;
using System.Collections;
using UnityEngine;

public enum RectOptions {
    Center = 0,
    BottomRight = 1,
    TopRight = 2,
    BottomLeft = 3,
    TopLeft = 4,
    //Top = 5,
    //Left = 6,
    //Right = 7,
    //Bottom = 8,
    Custom = 9
}

public class TextureUtils 
{
    // Based on https://gist.github.com/natsupy/e129936543f9b4663a37ea0762172b3b

    // Crop Squared Method
    public static IEnumerator CropSquare(WebCamTexture texture, RectOptions rectOptions, System.Action<Texture2D> callback) {
        var smallest = texture.width < texture.height ? texture.width : texture.height;
        var rect = new Rect (0, 0, smallest, smallest);

        if (rect.height < 0 || rect.width < 0) {
            Debug.LogWarning ("Invalid texture size");
        }

        Texture2D result = new Texture2D ((int)rect.width, (int)rect.height);

        if (rect.width != 0 && rect.height != 0) {
            float xRect = rect.x;
            float yRect = rect.y;
            float widthRect = rect.width;
            float heightRect = rect.height;

            switch (rectOptions) {
                case RectOptions.Center:
                    xRect = (texture.width - rect.width) / 2;
                    yRect = (texture.height - rect.height) / 2;
                    break;

                case RectOptions.BottomRight:
                    xRect = texture.width - rect.width;
                    break;

                case RectOptions.BottomLeft:
                    break;

                case RectOptions.TopLeft:
                    yRect = texture.height - rect.height;
                    break;

                case RectOptions.TopRight:
                    xRect = texture.width - rect.width;
                    yRect = texture.height - rect.height;
                    break;

                case RectOptions.Custom:
                    float tempWidth = texture.width - rect.width;
                    float tempHeight = texture.height - rect.height;
                    xRect = tempWidth > texture.width ? 0 : tempWidth;
                    yRect = tempHeight > texture.height ? 0 : tempHeight;
                    break;
            }

            if (texture.width < rect.x + rect.width || texture.height < rect.y + rect.height ||
                xRect > rect.x + texture.width || yRect > rect.y + texture.height ||
                xRect < 0 || yRect < 0 || rect.width < 0 || rect.height < 0) {
                throw new System.ArgumentException ("Set value crop less than origin texture size");
            }

            result.SetPixels (texture.GetPixels (Mathf.FloorToInt (xRect), Mathf.FloorToInt (yRect),
                                            Mathf.FloorToInt (widthRect), Mathf.FloorToInt (heightRect)));
            yield return null;
            result.Apply ();
        }

        yield return null;
        callback (result);
    }

    // Based on https://pastebin.com/qkkhWs2J

    // Scale Method
    public static Texture2D ScaleTexture(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear) {
        Rect texR = new Rect (0, 0, width, height);
        _gpu_scale (src, width, height, mode);

        //Get rendered data back to a new texture
        Texture2D result = new Texture2D (width, height, TextureFormat.ARGB32, true);
        result.Resize (width, height);
        result.ReadPixels (texR, 0, 0, true);
        return result;
    }

    // Internal utility that renders the source texture into the RTT - the scaling method itself.
    static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode) {
        //We need the source texture in VRAM because we render with it
        src.filterMode = fmode;
        src.Apply (true);

        //Using RTT for best quality and performance. Thanks, Unity 5
        RenderTexture rtt = new RenderTexture (width, height, 32);

        //Set the RTT in order to render to it
        Graphics.SetRenderTarget (rtt);

        //Setup 2D matrix in range 0..1, so nobody needs to care about sized
        GL.LoadPixelMatrix (0, 1, 1, 0);

        //Then clear & draw the texture to fill the entire RTT.
        GL.Clear (true, true, new Color (0, 0, 0, 0));
        Graphics.DrawTexture (new Rect (0, 0, 1, 1), src);
    }

    // Based on https://answers.unity.com/questions/685656/rotate-an-image-by-modifying-texture2dgetpixels32.html
    public static Color32[] RotateImageMatrix(Color32[] matrix, int width, int height, int angle) {
        Color32[] pix1 = new Color32[matrix.Length];

        int x = 0;
        int y = 0;

        Color32[] pix3 = rotateSquare (
            matrix, width, height, (Math.PI / 180 * (double)angle));

        for (int j = 0; j < height; j++) {
            for (var i = 0; i < width; i++) {
                pix1[x + i + width * (j + y)] = pix3[i + j * width];
            }
        }

        return pix3;
    }

    static Color32[] rotateSquare(Color32[] arr, int width, int height, double phi) {
        int x;
        int y;
        int i;
        int j;
        double sn = Math.Sin (phi);
        double cs = Math.Cos (phi);
        Color32[] arr2 = new Color32[arr.Length];

        int xc = width / 2;
        int yc = height / 2;

        for (j = 0; j < height; j++) {
            for (i = 0; i < width; i++) {
                arr2[j * width + i] = new Color32 (0, 0, 0, 0);
                x = (int)(cs * (i - xc) + sn * (j - yc) + xc);
                y = (int)(-sn * (i - xc) + cs * (j - yc) + yc);
                if ((x > -1) && (x < width) && (y > -1) && (y < height)) {
                    arr2[j * width + i] = arr[y * width + x];
                }
            }
        }
        return arr2;
    }

}
