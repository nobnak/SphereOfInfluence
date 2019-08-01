#ifndef __OCCUPY_CGINC__
#define __OCCUPY_CGINC__

static const float DEF_SIGMA = 0.2;

float4 SoftMax(float4 w, float sigma = DEF_SIGMA) {
	float wmax = max(max(w.x, w.y), max(w.z, w.w));
	float4 ew = exp((w - wmax) / sigma);
	float esum = ew.x + ew.y + ew.z + ew.w;
	return esum > 0 ? (ew / esum) : 0;
}
float4 SoftMin(float4 w, float sigma = DEF_SIGMA) {
	return SoftMax(-w, sigma);
}

#endif