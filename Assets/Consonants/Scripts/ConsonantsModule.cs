using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConsonantsModule : MonoBehaviour {
	public const int WIDTH = 5;
	public const int HEIGHT = 3;
	public const int LETTERS_COUNT = WIDTH * HEIGHT;
	public const float X_OFFSET = .025f;
	public const float Z_OFFSET = .025f;

	private static int moduleIdCounter = 1;
	private static readonly HashSet<char> VOWELS = new HashSet<char>(new[] { 'A', 'E', 'I', 'O', 'U' });
	private static readonly HashSet<char> EXCLUDE = new HashSet<char>(new[] { 'Y', 'M', 'W' });
	private static readonly HashSet<char> CONSONANTS = new HashSet<char>(Enumerable.Range('A', 'Z' - 'A' + 1).Select(c => (char)c).Where(c => (
		!VOWELS.Contains(c) && !EXCLUDE.Contains(c)
	)));

	public readonly string TwitchHelpMessage = "TODO: Write";

	public Renderer BackgroundRenderer;
	public GameObject LettersContainer;
	public KMNeedyModule Needy;
	public KMSelectable Selectable;
	public KMAudio Audio;
	public LetterComponent LetterPrefab;

	private bool onceActivated = false;
	private bool activated = false;
	private bool skip;
	private int moduleId;
	private float activationTime;
	private LetterComponent[] letters;

	private void Start() {
		List<LetterComponent> letters = new List<LetterComponent>();
		for (int x = 0; x < WIDTH; x++) {
			for (int z = 0; z < HEIGHT; z++) {
				LetterComponent letter = Instantiate(LetterPrefab);
				letter.transform.parent = LettersContainer.transform;
				letter.transform.localPosition = new Vector3(X_OFFSET * (x - WIDTH / 2f + .5f), 0f, Z_OFFSET * (z - HEIGHT / 2f + .5f));
				letter.transform.localRotation = Quaternion.identity;
				letter.transform.localScale = Vector3.one;
				letter.Selectable.Parent = Selectable;
				letters.Add(letter);
			}
		}
		this.letters = letters.ToArray();
		Selectable.Children = letters.Select(l => l.Selectable).ToArray();
		foreach (LetterComponent letter in letters) {
			LetterComponent closure = letter;
			letter.Selectable.OnInteract += () => { OnLetterPressed(closure); return false; };
		}
		Selectable.UpdateChildren();
		moduleId = moduleIdCounter++;
		Needy.OnActivate += OnActivate;
	}

	private void Update() {
		Color warningColor = Color.green;
		if (!onceActivated) warningColor = new Color(1f, Mathf.Sin(Time.time * Mathf.PI) * 0.5f + 0.5f, 0f);
		else if (activated) warningColor = new Color(1f, Mathf.Sin(Mathf.PI * (activationTime + Mathf.Pow(Time.time - activationTime, 1.2f))) * 0.5f + 0.5f, 0f);
		BackgroundRenderer.material.SetColor("_UnlitTint", warningColor);
	}

	private void OnActivate() {
		Needy.OnNeedyActivation += OnNeedyActivation;
		Needy.OnNeedyDeactivation += OnNeedyDeactivation;
		Needy.OnTimerExpired += OnTimerExpired;
	}

	private void OnNeedyActivation() {
		int consonantsCount = Random.Range(1, 7);
		skip = consonantsCount % 2 == 1;
		HashSet<char> consonants = new HashSet<char>(CONSONANTS);
		List<char> chars = new List<char>();
		for (int i = 0; i < consonantsCount; i++) {
			char c = consonants.PickRandom();
			consonants.Remove(c);
			chars.Add(c);
		}
		List<char> consonantsList = new List<char>(chars);
		consonantsList.Sort();
		while (chars.Count < LETTERS_COUNT) chars.Add(VOWELS.PickRandom());
		char[] charsArray = chars.Shuffle().ToArray();
		Debug.LogFormat("[Consonants #{0}] Module activated. Consonants: {1}", moduleId, consonantsList.Join(""));
		for (int i = 0; i < LETTERS_COUNT; i++) letters[i].letter = charsArray[i];
		activationTime = Time.time;
		onceActivated = true;
		activated = true;
	}

	private void OnNeedyDeactivation() {
		activated = false;
		for (int i = 0; i < LETTERS_COUNT; i++) letters[i].letter = null;
	}

	private void OnTimerExpired() {
		if (!skip) {
			Needy.HandleStrike();
			Debug.LogFormat("[Consonants #{0}] Timer expired. Strike!", moduleId);
		} else {
			Debug.LogFormat("[Consonants #{0}] Time expired. Module deactivated", moduleId);
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		}
		OnNeedyDeactivation();
	}

	private void OnLetterPressed(LetterComponent letter) {
		if (letter.letter == null) return;
		if (skip) {
			Debug.LogFormat("[Consonants #{0}] Pressed letter when initial consonants count is odd. Strike!", moduleId);
			Needy.HandleStrike();
			return;
		}
		if (VOWELS.Contains(letter.letter.Value)) {
			Debug.LogFormat("[Consonants #{0}] Pressed vowel {1}. Strike!", moduleId, letter.letter.Value);
			Needy.HandleStrike();
			return;
		}
		LetterComponent missingLetter = letters.FirstOrDefault(l => l.letter != null && CONSONANTS.Contains(l.letter.Value) && l.letter.Value < letter.letter.Value);
		if (missingLetter != null) {
			Debug.LogFormat("[Consonants #{0}] Letter {1} pressed before {2}. Strike!", moduleId, letter.letter.Value, missingLetter.letter.Value);
			Needy.HandleStrike();
			return;
		}
		letter.letter = null;
		if (letters.All(l => l.letter == null || VOWELS.Contains(l.letter.Value))) {
			Debug.LogFormat("[Consonants #{0}] Module deactivated. Used time: {1}", moduleId, Time.time - activationTime);
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
			Needy.HandlePass();
			OnNeedyDeactivation();
		} else Audio.PlaySoundAtTransform("LetterPressed", transform);
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		command = command.Trim().ToLower();
		yield break;
	}

	private IEnumerator TwitchHandleForcedSolve() {
		yield return null;
	}
}
