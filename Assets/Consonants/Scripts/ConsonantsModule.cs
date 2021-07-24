using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ConsonantsModule : MonoBehaviour {
	public const int WIDTH = 5;
	public const int HEIGHT = 3;
	public const int LETTERS_COUNT = WIDTH * HEIGHT;
	public const float TWITCH_PLAYS_TIMER = 95f;
	public const float X_OFFSET = .025f;
	public const float Z_OFFSET = .025f;
	public const float BLACK_LETTER_UPDATING_INTERVAL_MIN = 5f;
	public const float BLACK_LETTER_UPDATING_INTERVAL_MAX = 10f;

	private static int moduleIdCounter = 1;
	private static readonly HashSet<char> VOWELS = new HashSet<char>(new[] { 'A', 'E', 'I', 'O', 'U' });
	private static readonly HashSet<char> EXCLUDE = new HashSet<char>(new[] { 'Y', 'M', 'W' });
	private static readonly HashSet<char> CONSONANTS = new HashSet<char>(Enumerable.Range('A', 'Z' - 'A' + 1).Select(c => (char)c).Where(c => (
		!VOWELS.Contains(c) && !EXCLUDE.Contains(c)
	)));

	public readonly string TwitchHelpMessage =
		"\"{0} press A1 B2 C3\" - press letters by their positions. Word \"press\" is optional. Top left is A1. Columns: A-E. Rows: 1-3";

	public Renderer BackgroundRenderer;
	public GameObject LettersContainer;
	public KMNeedyModule Needy;
	public KMSelectable Selectable;
	public KMAudio Audio;
	public LetterComponent LetterPrefab;

	public bool TwitchPlaysActive;

	private bool onceActivated = false;
	private bool activated = false;
	private bool even;
	private int moduleId;
	private float activationTime;
	private float nextBlackLetterUpdateTime;
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
		if (activated) {
			warningColor = new Color(1f, Mathf.Sin(Mathf.PI * (activationTime + Mathf.Pow(Time.time - activationTime, 1.2f))) * 0.5f + 0.5f, 0f);
			if (Time.time >= nextBlackLetterUpdateTime) {
				foreach (LetterComponent letter in letters) letter.Black = false;
				if (letters.All(l => l.letter != null)) letters.Where(l => VOWELS.Contains(l.letter.Value)).PickRandom().Black = true;
				nextBlackLetterUpdateTime = Time.time + Random.Range(BLACK_LETTER_UPDATING_INTERVAL_MIN, BLACK_LETTER_UPDATING_INTERVAL_MAX);
			}
		}
		BackgroundRenderer.material.SetColor("_UnlitTint", warningColor);
	}

	private void OnActivate() {
		Needy.OnNeedyActivation += OnNeedyActivation;
		Needy.OnNeedyDeactivation += OnNeedyDeactivation;
		Needy.OnTimerExpired += OnTimerExpired;
		if (TwitchPlaysActive) Needy.CountdownTime = TWITCH_PLAYS_TIMER;
	}

	private void OnNeedyActivation() {
		int consonantsCount = Random.Range(2, 8);
		even = consonantsCount % 2 == 0;
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
		nextBlackLetterUpdateTime = Time.time + Random.Range(BLACK_LETTER_UPDATING_INTERVAL_MIN, BLACK_LETTER_UPDATING_INTERVAL_MAX);
		foreach (LetterComponent letter in letters) letter.Black = false;
		onceActivated = true;
		activated = true;
	}

	private void OnNeedyDeactivation() {
		activated = false;
		for (int i = 0; i < LETTERS_COUNT; i++) letters[i].letter = null;
	}

	private void OnTimerExpired() {
		if (!even && letters.Count(l => l.letter == null) == 1) {
			Debug.LogFormat("[Consonants #{0}] Time expired. Module deactivated", moduleId);
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		} else {
			Debug.LogFormat("[Consonants #{0}] Timer expired. Strike!", moduleId);
			Needy.HandleStrike();
		}
		OnNeedyDeactivation();
	}

	private void OnLetterPressed(LetterComponent letter) {
		if (letter.letter == null) return;
		if (!even && letters.Any(l => l.letter == null)) {
			Debug.LogFormat("[Consonants #{0}] Pressed 2nd letter when initial consonants count is odd. Strike!", moduleId);
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
		if (command.StartsWith("press ")) command = command.Skip("press ".Length).Join("").Trim();
		if (!Regex.IsMatch(command, "^((^| +)[a-e][1-3])+$")) yield break;
		List<KMSelectable> result = new List<KMSelectable>();
		foreach (string coord in command.Split(' ').Where(s => s.Length > 0)) {
			int x = coord[0] - 'a';
			int z = '3' - coord[1];
			LetterComponent letter = letters[x * HEIGHT + z];
			result.Add(letter.Selectable);
		}
		yield return null;
		yield return result.ToArray();
	}

	private IEnumerator TwitchHandleForcedSolve() {
		yield return null;
	}
}
