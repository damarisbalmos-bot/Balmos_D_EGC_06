#version 330 core

in vec2 vUV;
out vec4 FragColor;

uniform float uTime;
uniform bool uShowUV;

uniform float uHour;
uniform float uMinute;
uniform float uSecond;
uniform float uNight;
uniform vec3  uFaceDay;
uniform vec3  uFaceNight;
uniform float uHandWidth;
uniform float uRimWidth;
uniform int   uTickStep;

const float PI = 3.14159265;

float sdSegment(vec2 p, vec2 a, vec2 b)
{
    vec2 pa = p - a, ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float fillAA(float d)
{
    float w = fwidth(d);
    return 1.0 - smoothstep(-w, w, d);
}

vec2 dirOf(float turns)
{
    float a = turns * 2.0 * PI;
    return vec2(sin(a), cos(a));
}

float hand(vec2 p, float turns, float tail, float len, float width)
{
    vec2 d = dirOf(turns);
    return fillAA(sdSegment(p, -d * tail, d * len) - width);
}

void main()
{
    vec2 p = (vUV - 0.5) * 2.0;
    float r = length(p);

    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    float R = 0.98;
    float disc = fillAA(r - R);
    if (disc <= 0.0)
    {
        FragColor = vec4(0.0);
        return;
    }

    vec3 face = mix(uFaceDay, uFaceNight, uNight);
    vec3 ink = mix(vec3(0.08), vec3(0.92), uNight);
    vec3 col = face * (1.0 - 0.18 * r * r);

    float ang = atan(p.x, p.y);
    float sector = mod(ang + 2.0 * PI, 2.0 * PI) / (2.0 * PI) * 60.0;
    float f = fract(sector + 0.5) - 0.5;
    int idx = int(floor(sector + 0.5)) % 60;
    bool isHour = (idx % 5) == 0;
    float across = abs(f) * (2.0 * PI / 60.0) * r;

    float width = isHour ? 0.018 : 0.007;
    float rIn = isHour ? 0.70 : 0.79;
    float rOut = 0.86;
    float shape = max(across - width, max(rIn - r, r - rOut));
    float tick = fillAA(shape);
    if (!isHour && (idx % max(uTickStep, 1)) != 0)
    {
        tick = 0.0;
    }
    col = mix(col, ink, tick);

    float hH = hand(p, uHour / 12.0, 0.06, 0.50, 0.030 * uHandWidth);
    float hM = hand(p, uMinute / 60.0, 0.06, 0.74, 0.020 * uHandWidth);
    col = mix(col, ink, hH);
    col = mix(col, ink, hM);

    vec3 red = vec3(0.85, 0.12, 0.10);
    float hS = hand(p, uSecond / 60.0, 0.18, 0.82, 0.006 * uHandWidth);
    float cap = fillAA(r - 0.04);
    col = mix(col, red, max(hS, cap));

    float rim = 1.0 - fillAA(r - (R - uRimWidth));
    float shine = 0.5 + 0.5 * sin(ang * 2.0 + uTime * 0.5);
    vec3 gold = mix(vec3(0.55, 0.42, 0.15), vec3(0.95, 0.80, 0.40), shine);
    col = mix(col, gold, rim);

    FragColor = vec4(col, disc);
}