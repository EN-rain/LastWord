shader_type spatial;
render_mode unshaded, depth_test_disabled;

uniform vec4 albedo : source_color = vec4(1.0, 0.2, 0.2, 1.0);
uniform float pulse_speed = 2.0;

void fragment() {
    float pulse = (sin(TIME * pulse_speed) + 1.0) * 0.5;
    ALBEDO = albedo.rgb;
    ALPHA = mix(0.5, 1.0, pulse);
}
