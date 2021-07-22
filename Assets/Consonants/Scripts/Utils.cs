public static class Utils {
	public static float Mod(float x, float m) {
		return x < 0 ? ((x % m) + m) % m : x % m;
	}
}
