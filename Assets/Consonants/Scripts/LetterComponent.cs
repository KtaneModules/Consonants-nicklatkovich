using UnityEngine;

public class LetterComponent : MonoBehaviour {
	public const float MIN_SPEED = 3f;
	public const float MAX_SPEED = 5f;
	public const float MIN_STAGE_LENGTH = 1f;
	public const float MAX_STAGE_LENGTH = 2f;

	private readonly Color[] colors = new[] { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

	public bool Black = false;
	public KMSelectable Selectable;
	public TextMesh Text;

	private char? _letter;
	public char? letter { get { return _letter; } set { _letter = value; UpdateText(); } }

	private float anim = 1f;
	private float stageProgress = 0f;
	private float stageLength = 0f;
	private float speed;
	private int rotationFrom;
	private int rotationTo;
	private Color colorFrom;
	private Color colorTo;

	private void Start() {
		UpdateText();
	}

	private void Update() {
		if (letter == null) return;
		stageProgress += Time.deltaTime;
		if (anim < 1) anim = Mathf.Min(1, anim + speed * Time.deltaTime);
		else if (stageProgress >= stageLength) {
			rotationFrom = rotationTo;
			colorFrom = colorTo;
			rotationTo = rotationFrom + Random.Range(-2, 3);
			colorTo = Black ? Color.black : colors.PickRandom();
			anim = 0f;
			speed = Random.Range(MIN_SPEED, MAX_SPEED);
			stageProgress = 0f;
			stageLength = Random.Range(MIN_STAGE_LENGTH, MAX_STAGE_LENGTH);
		}
		Text.transform.localRotation = Quaternion.Euler(90f, 90f * (rotationFrom + anim * (rotationTo - rotationFrom)), 0);
		Text.color = colorFrom + anim * (colorTo - colorFrom);
	}

	private void UpdateText() {
		Text.text = letter == null ? "" : letter.Value.ToString();
		if (letter == null) return;
		rotationFrom = Random.Range(0, 4);
		stageLength = Random.Range(MIN_STAGE_LENGTH, MAX_STAGE_LENGTH);
		stageProgress = Random.Range(0, stageLength);
		anim = 1;
		colorFrom = colorTo = Color.black;
	}
}
