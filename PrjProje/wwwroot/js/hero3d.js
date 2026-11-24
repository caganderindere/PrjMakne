// Home hero: a powder coating line in three.js, driven by the scroll.
// Sheet-metal parts hang from the overhead conveyor, enter the booth bare, come out with a matte powder coat,
// cure in the tunnel oven and leave glossy red. Modelled after the company's own installations: red-framed
// booth with a cyclone and yellow gun reciprocators, grey-panelled oven with red corner posts.
import * as THREE from "../lib/three/three.module.min.js";
import { RoomEnvironment } from "../lib/three/RoomEnvironment.js";
import { mergeGeometries } from "../lib/three/BufferGeometryUtils.js";

const section = document.querySelector("[data-paint-line]");
const host = section && section.querySelector(".hero-line-canvas");
if (host) {
    try {
        start();
    } catch (error) {
        // No WebGL: the poster photo stays.
        console.warn("Paint line hero disabled:", error);
    }
}

function start() {
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const mobile = window.matchMedia("(max-width: 767px)").matches;
    const stage = section.querySelector(".hero-line-stage");
    const steps = section.querySelectorAll(".hero-line-steps li");

    const renderer = new THREE.WebGLRenderer({ antialias: true, powerPreference: "high-performance" });
    // Resolution starts here and is lowered on its own if the GPU cannot keep up (see the loop).
    const maxPixelRatio = Math.min(window.devicePixelRatio, mobile ? 1.5 : 1.75);
    let pixelRatio = maxPixelRatio;
    renderer.setPixelRatio(pixelRatio);
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 0.82;
    // The shader error check reads the compile log synchronously and stalls frames; the shaders here are fixed.
    renderer.debug.checkShaderErrors = false;
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFShadowMap;
    host.appendChild(renderer.domElement);

    const scene = new THREE.Scene();
    const hall = new THREE.Color(0x161a1f);
    scene.background = hall;
    scene.fog = new THREE.Fog(hall, 26, 62);
    const pmrem = new THREE.PMREMGenerator(renderer);
    scene.environment = pmrem.fromScene(new RoomEnvironment(renderer), 0.04).texture;

    const camera = new THREE.PerspectiveCamera(32, 2, 0.1, 120);

    // ---------- Materials ----------
    const std = (color, roughness, metalness, extra) =>
        new THREE.MeshStandardMaterial(Object.assign({ color, roughness, metalness, envMapIntensity: 0.6 }, extra));
    const M = {
        red: std(0xc50a14, 0.42, 0.25),
        panel: std(0xd9dde1, 0.6, 0.05),
        ovenPanel: std(0xa7afb8, 0.45, 0.45),
        galv: std(0xa9b1b9, 0.38, 0.85),
        steel: std(0x5b646e, 0.5, 0.7),
        stainless: std(0xcfd4d9, 0.22, 0.95),
        dark: std(0x1d2228, 0.8, 0.2),
        grate: std(0x2c3238, 0.7, 0.6),
        yellow: std(0xf0b400, 0.45, 0.1),
        black: std(0x111417, 0.6, 0.2),
        rubber: std(0x22262b, 0.9, 0),
        seam: std(0x7f8892, 0.5, 0.5),
    };
    const glowMaterial = new THREE.MeshBasicMaterial({ color: 0xff7a1a, transparent: true, opacity: 0.85 });
    const lampMaterial = new THREE.MeshBasicMaterial({ color: 0xfff6e8 });
    const boothLightMaterial = new THREE.MeshBasicMaterial({ color: 0xf4f8ff });

    const mesh = (geometry, material, x, y, z, parent, shadows = true) => {
        const m = new THREE.Mesh(geometry, material);
        m.position.set(x, y, z);
        m.castShadow = shadows;
        m.receiveShadow = shadows;
        (parent || scene).add(m);
        return m;
    };
    const box = (w, h, d, material, x, y, z, parent, shadows) => mesh(new THREE.BoxGeometry(w, h, d), material, x, y, z, parent, shadows);
    const cyl = (rt, rb, h, material, x, y, z, parent, segments = 32) => mesh(new THREE.CylinderGeometry(rt, rb, h, segments), material, x, y, z, parent);

    const canvasTexture = (w, h, draw) => {
        const canvas = document.createElement("canvas");
        canvas.width = w;
        canvas.height = h;
        draw(canvas.getContext("2d"), w, h);
        const texture = new THREE.CanvasTexture(canvas);
        texture.colorSpace = THREE.SRGBColorSpace;
        texture.anisotropy = 4;
        return texture;
    };

    // ---------- Hall ----------
    const concrete = canvasTexture(512, 512, (g, w, h) => {
        g.fillStyle = "#6b7177";
        g.fillRect(0, 0, w, h);
        for (let i = 0; i < 9000; i++) {
            const v = 90 + Math.random() * 40;
            g.fillStyle = `rgba(${v},${v + 3},${v + 6},${Math.random() * 0.35})`;
            g.fillRect(Math.random() * w, Math.random() * h, 1 + Math.random() * 2, 1 + Math.random() * 2);
        }
        g.strokeStyle = "rgba(30,34,38,.35)";
        g.lineWidth = 2;
        g.strokeRect(0, 0, w, h);
    });
    concrete.wrapS = concrete.wrapT = THREE.RepeatWrapping;
    concrete.repeat.set(20, 10);
    const floor = mesh(new THREE.PlaneGeometry(120, 60), std(0xffffff, 0.62, 0.05, { map: concrete, envMapIntensity: 0.35 }), 0, 0, 0, scene);
    floor.rotation.x = -Math.PI / 2;
    floor.castShadow = false;
    // Walkway markings.
    for (const z of [2.7, -2.9]) {
        const line = mesh(new THREE.PlaneGeometry(60, 0.09), new THREE.MeshStandardMaterial({ color: 0xe8b000, roughness: 0.6 }), 0, 0.004, z, scene, false);
        line.rotation.x = -Math.PI / 2;
        line.receiveShadow = true;
    }

    const wallTexture = canvasTexture(256, 256, (g, w, h) => {
        g.fillStyle = "#353b43";
        g.fillRect(0, 0, w, h);
        for (let x = 0; x < w; x += 32) {
            g.fillStyle = "rgba(255,255,255,.05)";
            g.fillRect(x, 0, 3, h);
            g.fillStyle = "rgba(0,0,0,.25)";
            g.fillRect(x + 3, 0, 2, h);
        }
    });
    wallTexture.wrapS = wallTexture.wrapT = THREE.RepeatWrapping;
    wallTexture.repeat.set(24, 3);
    mesh(new THREE.PlaneGeometry(120, 12), std(0x9aa1a8, 0.85, 0.3, { map: wallTexture }), 0, 6, -12, scene, false);
    // Clerestory windows and high-bay lamps.
    mesh(new THREE.PlaneGeometry(120, 1.1), new THREE.MeshBasicMaterial({ color: 0x55657a }), 0, 10.2, -11.95, scene, false);
    for (let x = -32; x <= 32; x += 8) {
        box(0.4, 12, 0.4, M.steel, x, 6, -11.6, scene);
        box(0.25, 0.25, 22, M.steel, x, 11.6, 1, scene, false);
        const lamp = cyl(0.35, 0.5, 0.35, M.steel, x + 4, 10.4, 0.5, scene, 20);
        lamp.castShadow = false;
        const bulb = mesh(new THREE.CircleGeometry(0.42, 24), lampMaterial, x + 4, 10.22, 0.5, scene, false);
        bulb.rotation.x = Math.PI / 2;
    }

    // ---------- Lights ----------
    scene.add(new THREE.HemisphereLight(0xdfe8f2, 0x1a1e23, 0.45));
    const sun = new THREE.DirectionalLight(0xfff3e2, 2.1);
    sun.position.set(9, 15, 9);
    sun.castShadow = true;
    sun.shadow.mapSize.set(1024, 1024);
    Object.assign(sun.shadow.camera, { left: -13, right: 13, top: 8, bottom: -5, near: 1, far: 45 });
    sun.shadow.radius = 2;
    sun.shadow.bias = -0.0004;
    sun.shadow.normalBias = 0.02;
    scene.add(sun);
    const fill = new THREE.DirectionalLight(0xb8cde6, 0.6);
    fill.position.set(-10, 8, 6);
    scene.add(fill);
    for (const x of [-8, 0, 8]) {
        const bay = new THREE.PointLight(0xfff1dc, 40, 26, 2);
        bay.position.set(x + 4, 9.8, 0.5);
        scene.add(bay);
    }

    // ---------- Overhead conveyor ----------
    const railY = 4.6;
    const beamShape = new THREE.Shape();
    // I-beam section (0.16 wide, 0.22 tall), extruded along x.
    beamShape.moveTo(-0.08, -0.11).lineTo(0.08, -0.11).lineTo(0.08, -0.09).lineTo(0.012, -0.09).lineTo(0.012, 0.09)
        .lineTo(0.08, 0.09).lineTo(0.08, 0.11).lineTo(-0.08, 0.11).lineTo(-0.08, 0.09).lineTo(-0.012, 0.09)
        .lineTo(-0.012, -0.09).lineTo(-0.08, -0.09).closePath();
    const beam = mesh(new THREE.ExtrudeGeometry(beamShape, { depth: 60, bevelEnabled: false }), M.galv, -30, railY, 0, scene);
    beam.rotation.y = Math.PI / 2;
    box(60, 0.05, 0.05, M.dark, 0, railY - 0.16, 0, scene);
    // Supports: red portal frames behind the line, kept clear of the machines.
    for (let x = -18; x <= 18; x += 4.5) {
        if (x > -6 && x < 7) continue;
        box(0.14, railY + 0.3, 0.14, M.red, x, (railY + 0.3) / 2, -1.6, scene);
        box(0.12, 0.12, 1.7, M.red, x, railY + 0.25, -0.8, scene);
        box(0.05, 0.4, 0.05, M.galv, x, railY + 0.07, 0, scene);
    }
    // Chain trolleys, moved with the scroll.
    const trolleyPitch = 0.6;
    const trolleyCount = Math.ceil(60 / trolleyPitch);
    const trolleys = new THREE.InstancedMesh(new THREE.BoxGeometry(0.12, 0.08, 0.1), M.steel, trolleyCount);
    trolleys.castShadow = true;
    scene.add(trolleys);
    const helper = new THREE.Object3D();
    const placeTrolleys = (offset) => {
        const shift = ((offset % trolleyPitch) + trolleyPitch) % trolleyPitch;
        for (let i = 0; i < trolleyCount; i++) {
            helper.position.set(-30 + i * trolleyPitch + shift, railY - 0.2, 0);
            helper.updateMatrix();
            trolleys.setMatrixAt(i, helper.matrix);
        }
        trolleys.instanceMatrix.needsUpdate = true;
    };

    // ---------- Wall with openings (end walls of booth and oven) ----------
    const endWall = (depth, height, openW, openBottom, openTop, material, x, cx) => {
        const s = new THREE.Shape();
        s.moveTo(-depth / 2, 0).lineTo(depth / 2, 0).lineTo(depth / 2, height).lineTo(0.05, height).lineTo(0.05, openTop)
            .lineTo(openW / 2, openTop).lineTo(openW / 2, openBottom).lineTo(-openW / 2, openBottom).lineTo(-openW / 2, openTop)
            .lineTo(-0.05, openTop).lineTo(-0.05, height).lineTo(-depth / 2, height).closePath();
        const wall = mesh(new THREE.ExtrudeGeometry(s, { depth: 0.06, bevelEnabled: false }), material, cx + x, 0, 0, scene);
        wall.rotation.y = Math.PI / 2;
        return wall;
    };
    const frameBox = (cx, cy, cz, w, h, d, t, material) => {
        // The twelve edges of a box.
        for (const sy of [-1, 1]) for (const sz of [-1, 1]) box(w + t, t, t, material, cx, cy + sy * h / 2, cz + sz * d / 2, scene);
        for (const sx of [-1, 1]) for (const sz of [-1, 1]) box(t, h, t, material, cx + sx * w / 2, cy, cz + sz * d / 2, scene);
        for (const sx of [-1, 1]) for (const sy of [-1, 1]) box(t, t, d, material, cx + sx * w / 2, cy + sy * h / 2, cz, scene);
    };

    // ---------- Powder booth ----------
    const booth = { x: -3.5, len: 3.2, depth: 2.6, height: 3.4 };
    const bx0 = booth.x - booth.len / 2;
    const bx1 = booth.x + booth.len / 2;
    box(booth.len + 0.3, 0.18, booth.depth + 0.3, M.dark, booth.x, 0.09, 0, scene);
    box(booth.len, 0.02, booth.depth - 0.1, M.grate, booth.x, 0.19, 0, scene);
    box(booth.len, booth.height, 0.05, M.panel, booth.x, booth.height / 2 + 0.18, -booth.depth / 2, scene);
    // Front wall with a wide operator opening so the coating can be seen.
    {
        const s = new THREE.Shape();
        const w = booth.len, h = booth.height;
        s.moveTo(-w / 2, 0).lineTo(w / 2, 0).lineTo(w / 2, h).lineTo(-w / 2, h).closePath();
        const hole = new THREE.Path();
        hole.moveTo(-w / 2 + 0.3, 0.55).lineTo(w / 2 - 0.3, 0.55).lineTo(w / 2 - 0.3, 2.75).lineTo(-w / 2 + 0.3, 2.75).closePath();
        s.holes.push(hole);
        mesh(new THREE.ExtrudeGeometry(s, { depth: 0.05, bevelEnabled: false }), M.panel, booth.x, 0.18, booth.depth / 2 - 0.05, scene);
    }
    box(booth.len - 0.1, 0.35, 0.03, M.steel, booth.x, 0.36, booth.depth / 2 + 0.01, scene);
    for (const dx of [-booth.len / 2 + 0.15, booth.len / 2 - 0.15]) box(0.02, booth.height - 0.2, 0.02, M.seam, booth.x + dx, booth.height / 2 + 0.18, booth.depth / 2 + 0.01, scene, false);
    box(booth.len - 0.6, 0.06, 0.06, M.steel, booth.x, 2.95, booth.depth / 2 + 0.01, scene);
    endWall(booth.depth, booth.height, 0.7, 0.75, 2.75, M.panel, bx0, 0).position.y = 0.18;
    endWall(booth.depth, booth.height, 0.7, 0.75, 2.75, M.panel, bx1 - 0.06, 0).position.y = 0.18;
    box(booth.len, 0.05, booth.depth / 2 - 0.05, M.panel, booth.x, booth.height + 0.18, -booth.depth / 4 - 0.025, scene);
    box(booth.len, 0.05, booth.depth / 2 - 0.05, M.panel, booth.x, booth.height + 0.18, booth.depth / 4 + 0.025, scene);
    frameBox(booth.x, booth.height / 2 + 0.18, 0, booth.len, booth.height, booth.depth, 0.09, M.red);
    // Light strips inside and a cool light for the coating zone.
    box(booth.len - 0.3, 0.05, 0.12, boothLightMaterial, booth.x, booth.height + 0.1, booth.depth / 2 - 0.25, scene, false);
    box(booth.len - 0.3, 0.05, 0.12, boothLightMaterial, booth.x, booth.height + 0.1, -booth.depth / 2 + 0.25, scene, false);
    const boothLight = new THREE.PointLight(0xeef4ff, 14, 6, 2);
    boothLight.position.set(booth.x, 2.9, 0.4);
    scene.add(boothLight);
    // Gun slots in the back wall and the reciprocator behind it.
    box(0.12, 1.6, 0.06, M.black, booth.x - 0.4, 1.75, -booth.depth / 2 + 0.01, scene);
    box(0.12, 1.6, 0.06, M.black, booth.x + 0.5, 1.75, -booth.depth / 2 + 0.01, scene);
    const guns = [];
    const carriages = [];
    for (const gx of [booth.x - 0.4, booth.x + 0.5]) {
        box(0.6, 0.12, 0.6, M.yellow, gx, 0.06, -booth.depth / 2 - 0.55, scene);
        box(0.22, 3.2, 0.22, M.yellow, gx, 1.7, -booth.depth / 2 - 0.55, scene);
        box(0.3, 0.3, 0.3, M.dark, gx, 3.4, -booth.depth / 2 - 0.55, scene);
        const carriage = new THREE.Group();
        carriage.position.set(gx, 1.75, -booth.depth / 2 - 0.3);
        scene.add(carriage);
        box(0.26, 0.9, 0.18, M.yellow, 0, 0, -0.12, carriage);
        for (const gy of [-0.3, 0.3]) {
            const gun = cyl(0.035, 0.035, 0.7, M.dark, 0, gy, 0.3, carriage, 16);
            gun.rotation.x = Math.PI / 2;
            const tip = cyl(0.012, 0.035, 0.08, M.galv, 0, gy, 0.69, carriage, 12);
            tip.rotation.x = Math.PI / 2;
            guns.push({ carriage, y: gy, x: 0, z: 0.75 });
        }
        carriages.push(carriage);
    }
    // Powder centre with its hoses, as in the installations.
    box(0.7, 1.15, 0.6, M.yellow, booth.x - 2.4, 0.58, 1.75, scene);
    box(0.5, 0.35, 0.05, M.dark, booth.x - 2.4, 1.0, 2.06, scene);
    cyl(0.2, 0.2, 0.5, M.stainless, booth.x - 2.4, 1.4, 1.75, scene, 24);
    const hose = new THREE.CatmullRomCurve3([
        new THREE.Vector3(booth.x - 2.4, 1.2, 1.5), new THREE.Vector3(booth.x - 2.6, 0.4, 0), new THREE.Vector3(booth.x - 1.5, 0.15, -1.9), new THREE.Vector3(booth.x - 0.4, 0.4, -1.9),
    ]);
    mesh(new THREE.TubeGeometry(hose, 40, 0.025, 8), M.rubber, 0, 0, 0, scene);

    // Cyclone recovery and after-filter behind the booth.
    const cyc = { x: -4.6, z: -4.3 };
    cyl(0.6, 0.6, 1.7, M.stainless, cyc.x, 3.35, cyc.z, scene, 40);
    cyl(0.6, 0.14, 1.5, M.stainless, cyc.x, 1.75, cyc.z, scene, 40);
    cyl(0.18, 0.18, 0.7, M.galv, cyc.x, 4.5, cyc.z, scene, 24);
    cyl(0.22, 0.22, 0.45, M.stainless, cyc.x, 0.75, cyc.z, scene, 24);
    for (const [dx, dz] of [[-0.6, -0.6], [0.6, -0.6], [-0.6, 0.6], [0.6, 0.6]]) box(0.08, 2.7, 0.08, M.red, cyc.x + dx, 1.35, cyc.z + dz, scene);
    box(1.3, 0.08, 1.3, M.red, cyc.x, 2.6, cyc.z, scene);
    const duct = new THREE.CatmullRomCurve3([
        new THREE.Vector3(booth.x + 0.6, booth.height + 0.2, -0.7), new THREE.Vector3(booth.x + 0.7, booth.height + 0.7, -2.2),
        new THREE.Vector3(cyc.x + 1.0, 3.9, cyc.z + 0.3), new THREE.Vector3(cyc.x + 0.55, 3.7, cyc.z),
    ]);
    mesh(new THREE.TubeGeometry(duct, 48, 0.22, 20), M.stainless, 0, 0, 0, scene);
    const filter = { x: -7.0, z: -4.6 };
    box(1.6, 3.2, 1.4, M.ovenPanel, filter.x, 1.6, filter.z, scene);
    frameBox(filter.x, 1.6, filter.z, 1.6, 3.2, 1.4, 0.08, M.red);
    cyl(0.25, 0.25, 2.4, M.galv, filter.x, 4.4, filter.z, scene, 24);
    const filterDuct = new THREE.CatmullRomCurve3([
        new THREE.Vector3(cyc.x, 4.8, cyc.z), new THREE.Vector3(cyc.x - 0.4, 5.3, cyc.z), new THREE.Vector3(filter.x + 0.5, 5.1, filter.z), new THREE.Vector3(filter.x + 0.4, 3.3, filter.z),
    ]);
    mesh(new THREE.TubeGeometry(filterDuct, 40, 0.16, 16), M.stainless, 0, 0, 0, scene);

    // ---------- Tunnel curing oven ----------
    const oven = { x: 2.6, len: 7.2, depth: 2.9, height: 3.6 };
    const ox0 = oven.x - oven.len / 2;
    const ox1 = oven.x + oven.len / 2;
    box(oven.len, oven.height, 0.12, M.ovenPanel, oven.x, oven.height / 2, oven.depth / 2 - 0.06, scene);
    box(oven.len, oven.height, 0.12, M.ovenPanel, oven.x, oven.height / 2, -oven.depth / 2 + 0.06, scene);
    box(oven.len, 0.12, oven.depth / 2 - 0.06, M.ovenPanel, oven.x, oven.height, -oven.depth / 4 - 0.03, scene);
    box(oven.len, 0.12, oven.depth / 2 - 0.06, M.ovenPanel, oven.x, oven.height, oven.depth / 4 + 0.03, scene);
    endWall(oven.depth, oven.height, 0.75, 0.7, 2.85, M.ovenPanel, ox0, 0);
    endWall(oven.depth, oven.height, 0.75, 0.7, 2.85, M.ovenPanel, ox1 - 0.06, 0);
    frameBox(oven.x, oven.height / 2, 0, oven.len, oven.height, oven.depth, 0.11, M.red);
    // Panel seams and the red mid posts of the front.
    for (let x = ox0 + 0.9; x < ox1 - 0.3; x += 0.9) box(0.018, oven.height - 0.2, 0.02, M.seam, x, oven.height / 2, oven.depth / 2 + 0.005, scene, false);
    box(oven.len + 0.1, 0.16, 0.06, M.dark, oven.x, 0.08, oven.depth / 2 + 0.03, scene);
    // Heat glowing in the openings.
    for (const x of [ox0 + 0.35, ox1 - 0.35]) {
        const glow = mesh(new THREE.PlaneGeometry(0.72, 2.1), glowMaterial, x, 1.78, 0, scene, false);
        glow.rotation.y = Math.PI / 2;
    }
    const ovenLights = [ox0 + 0.6, ox1 - 0.6].map((x) => {
        const light = new THREE.PointLight(0xff7a24, 6, 4, 2);
        light.position.set(x, 1.8, 0);
        scene.add(light);
        return light;
    });
    // Burner unit, circulation fan and exhaust stack on the roof.
    box(1.8, 1.1, 1.5, M.ovenPanel, ox1 - 1.6, oven.height + 0.6, -0.4, scene);
    frameBox(ox1 - 1.6, oven.height + 0.6, -0.4, 1.8, 1.1, 1.5, 0.07, M.red);
    cyl(0.32, 0.32, 0.5, M.steel, ox1 - 2.9, oven.height + 0.31, -0.6, scene, 28);
    cyl(0.14, 0.14, 2.6, M.galv, ox1 - 1.0, oven.height + 1.9, -0.6, scene, 24);
    cyl(0.01, 0.32, 0.22, M.galv, ox1 - 1.0, oven.height + 3.35, -0.6, scene, 24);
    // Control panel with a live temperature display.
    const panelX = ox1 - 0.9;
    box(0.62, 0.95, 0.22, M.ovenPanel, panelX, 1.7, oven.depth / 2 + 0.11, scene);
    frameBox(panelX, 1.7, oven.depth / 2 + 0.11, 0.62, 0.95, 0.22, 0.03, M.steel);
    let displayValue = -1;
    const displayCanvas = document.createElement("canvas");
    displayCanvas.width = 256;
    displayCanvas.height = 96;
    const displayTexture = new THREE.CanvasTexture(displayCanvas);
    displayTexture.colorSpace = THREE.SRGBColorSpace;
    const drawDisplay = (value) => {
        if (value === displayValue) return;
        displayValue = value;
        const g = displayCanvas.getContext("2d");
        g.fillStyle = "#07090b";
        g.fillRect(0, 0, 256, 96);
        g.fillStyle = "#ff8a3d";
        g.font = "bold 64px monospace";
        g.textAlign = "center";
        g.fillText(value + "°C", 128, 72);
        displayTexture.needsUpdate = true;
    };
    drawDisplay(180);
    mesh(new THREE.PlaneGeometry(0.42, 0.16), new THREE.MeshBasicMaterial({ map: displayTexture }), panelX, 1.95, oven.depth / 2 + 0.225, scene, false);
    const lamps = [0x3ddc84, 0xffb020, 0xe30613].map((color, i) =>
        Object.assign(mesh(new THREE.SphereGeometry(0.035, 16, 12), new THREE.MeshBasicMaterial({ color }), panelX - 0.15 + i * 0.15, 1.65, oven.depth / 2 + 0.23, scene, false), { userData: { dynamic: true } }));
    box(0.08, 0.16, 0.06, M.red, panelX, 1.4, oven.depth / 2 + 0.24, scene);
    // The company logo, large in the middle of the oven front (drawn from the site's own SVG).
    const logoHeight = 2.1;
    const logoImage = new Image();
    logoImage.onload = () => {
        // The SVG has only a viewBox (265 x 198), so its size is given here.
        const aspect = 265 / 198;
        const logoTexture = canvasTexture(Math.round(1024 * aspect), 1024, (g, w, h) => g.drawImage(logoImage, 0, 0, w, h));
        mesh(new THREE.PlaneGeometry(logoHeight * aspect, logoHeight),
            new THREE.MeshStandardMaterial({ map: logoTexture, transparent: true, alphaTest: 0.05, roughness: 0.45, metalness: 0.1, toneMapped: false }),
            oven.x, oven.height / 2 + 0.05, oven.depth / 2 + 0.03, scene, false);
        if (!running) render(0, true);
    };
    logoImage.src = "/images/brand/prj-logo.svg";

    // ---------- Parts on hangers ----------
    const partShape = new THREE.Shape();
    const pw = 1.0, ph = 1.4;
    partShape.moveTo(-pw / 2, 0).lineTo(pw / 2, 0).lineTo(pw / 2, -ph).lineTo(-pw / 2, -ph).closePath();
    const louver = (y) => {
        const p = new THREE.Path();
        p.absellipse(0, y, 0.3, 0.025, 0, Math.PI * 2, false, 0);
        return p;
    };
    for (let i = 0; i < 5; i++) partShape.holes.push(louver(-0.32 - i * 0.11));
    const hangHole = new THREE.Path();
    hangHole.absarc(0, -0.08, 0.035, 0, Math.PI * 2, false);
    partShape.holes.push(hangHole);
    const partGeometry = new THREE.ExtrudeGeometry(partShape, { depth: 0.025, bevelEnabled: true, bevelThickness: 0.004, bevelSize: 0.004, bevelSegments: 2, curveSegments: 24 });
    partGeometry.translate(0, 0, -0.0125);
    const flangeGeometry = new THREE.BoxGeometry(0.025, ph, 0.12);
    const lipGeometry = new THREE.BoxGeometry(pw, 0.025, 0.12);

    const raw = { color: new THREE.Color(0xbfc6cd), roughness: 0.3, metalness: 0.9 };
    const powder = { color: new THREE.Color(0xd9555b), roughness: 0.92, metalness: 0 };
    const cured = { color: new THREE.Color(0xc0060f), roughness: 0.3, metalness: 0.05 };
    const partTop = 2.45;
    const parts = [];
    const partSpacing = 5.5;
    for (let i = 0; i < 4; i++) {
        const material = new THREE.MeshPhysicalMaterial({ color: raw.color.clone(), roughness: raw.roughness, metalness: raw.metalness, clearcoat: 0.01, clearcoatRoughness: 0.15, envMapIntensity: 1, emissive: 0xff5a1a, emissiveIntensity: 0 });
        const group = new THREE.Group();
        scene.add(group);
        const body = new THREE.Group();
        body.position.y = partTop;
        group.add(body);
        mesh(partGeometry, material, 0, 0, 0, body);
        mesh(flangeGeometry, material, -pw / 2, -ph / 2, -0.06, body);
        mesh(flangeGeometry, material, pw / 2, -ph / 2, -0.06, body);
        mesh(lipGeometry, material, 0, -ph, -0.06, body);
        // Hanger rod and hook from the trolley into the hole.
        cyl(0.012, 0.012, railY - 0.25 - partTop - 0.05, M.galv, 0, (railY - 0.25 + partTop + 0.05) / 2, 0, group, 8);
        const hook = new THREE.CatmullRomCurve3([
            new THREE.Vector3(0, partTop + 0.06, 0), new THREE.Vector3(0.04, partTop - 0.02, 0.03), new THREE.Vector3(0, partTop - 0.09, 0.02), new THREE.Vector3(-0.03, partTop - 0.06, -0.02),
        ]);
        mesh(new THREE.TubeGeometry(hook, 16, 0.01, 6), M.galv, 0, 0, 0, group);
        box(0.14, 0.1, 0.12, M.steel, 0, railY - 0.2, 0, group);
        parts.push({ group, body, material, offset: -i * partSpacing });
    }

    // ---------- Powder spray ----------
    const sprayCount = mobile ? 500 : 1100;
    const sprayPositions = new Float32Array(sprayCount * 3);
    const sprayVelocity = new Float32Array(sprayCount * 3);
    const sprayLife = new Float32Array(sprayCount).fill(0);
    const sprayGeometry = new THREE.BufferGeometry();
    sprayGeometry.setAttribute("position", new THREE.BufferAttribute(sprayPositions, 3));
    const sprayMaterial = new THREE.PointsMaterial({ color: 0xe8414a, size: 0.04, transparent: true, opacity: 0.55, depthWrite: false, sizeAttenuation: true });
    const spray = new THREE.Points(sprayGeometry, sprayMaterial);
    spray.frustumCulled = false;
    scene.add(spray);
    const gunWorld = new THREE.Vector3();
    let sprayCursor = 0;
    let sprayWasAlive = true;
    const emit = (count) => {
        for (let n = 0; n < count; n++) {
            const gun = guns[(Math.random() * guns.length) | 0];
            gunWorld.set(gun.x, gun.y, gun.z);
            gun.carriage.localToWorld(gunWorld);
            const i = sprayCursor;
            sprayCursor = (sprayCursor + 1) % sprayCount;
            sprayPositions[i * 3] = gunWorld.x;
            sprayPositions[i * 3 + 1] = gunWorld.y;
            sprayPositions[i * 3 + 2] = gunWorld.z;
            sprayVelocity[i * 3] = (Math.random() - 0.5) * 0.9;
            sprayVelocity[i * 3 + 1] = (Math.random() - 0.5) * 0.7;
            sprayVelocity[i * 3 + 2] = 1.4 + Math.random() * 1.2;
            sprayLife[i] = 1;
        }
    };

    // ---------- Scroll, camera and animation ----------
    const smooth = (a, b, x) => {
        const t = Math.min(1, Math.max(0, (x - a) / (b - a)));
        return t * t * (3 - 2 * t);
    };
    const startX = -9.6;
    const travel = 19.4;
    let progress = reducedMotion ? 1 : 0;
    let camX = 0;
    const lookAt = new THREE.Vector3();

    const readProgress = () => {
        if (reducedMotion) return 1;
        const bounds = section.getBoundingClientRect();
        const span = bounds.height - stage.offsetHeight;
        return span > 0 ? Math.min(1, Math.max(0, (stage.getBoundingClientRect().top - bounds.top) / span)) : 1;
    };

    const update = (time, dt, snap) => {
        const chain = progress * travel;
        placeTrolleys(chain);
        let spraying = false;
        let inOven = false;
        let lead = 0;
        for (const part of parts) {
            const x = startX + chain + part.offset;
            part.group.position.x = x;
            part.group.visible = x > -16 && x < 16;
            // Gentle sway while moving.
            part.body.rotation.z = Math.sin(time * 1.3 + part.offset) * 0.012;
            const coat = smooth(bx0 + 0.4, bx1 - 0.5, x);
            const cure = smooth(ox0 + 0.8, ox1 - 0.6, x);
            const m = part.material;
            m.color.copy(raw.color).lerp(powder.color, coat).lerp(cured.color, cure);
            m.roughness = THREE.MathUtils.lerp(THREE.MathUtils.lerp(raw.roughness, powder.roughness, coat), cured.roughness, cure);
            m.metalness = THREE.MathUtils.lerp(THREE.MathUtils.lerp(raw.metalness, powder.metalness, coat), cured.metalness, cure);
            // Never exactly 0: a clearcoat of 0 compiles a different shader, which stalls a frame when it switches.
            m.clearcoat = 0.01 + 0.69 * cure;
            // Still warm for a moment after the oven.
            m.emissiveIntensity = x > ox1 ? Math.max(0, 1 - (x - ox1) / 2.2) * 0.18 : 0;
            if (x > bx0 + 0.2 && x < bx1 - 0.2) spraying = true;
            if (x > ox0 && x < ox1) inOven = true;
            if (part.offset === 0) lead = x;
        }

        // Steps follow the leading part.
        const active = lead < bx0 ? 0 : lead < ox0 ? 1 : lead < ox1 + 0.4 ? 2 : 3;
        steps.forEach((step, i) => {
            step.classList.toggle("is-active", i === active);
            step.classList.toggle("is-done", i < active);
        });
        section.classList.toggle("is-started", progress > 0.02);

        // Guns stroke up and down; powder flies while a part is in the booth.
        carriages.forEach((c, i) => { c.position.y = 1.75 + Math.sin(time * 2.2 + i * 1.7) * 0.28; });
        if (spraying && !reducedMotion) emit(Math.ceil(dt * (mobile ? 260 : 520)));
        let alive = 0;
        for (let i = 0; i < sprayCount; i++) {
            if (sprayLife[i] <= 0) {
                sprayPositions[i * 3 + 1] = -10;
                continue;
            }
            alive++;
            sprayLife[i] -= dt * 0.9;
            sprayVelocity[i * 3 + 2] *= 1 - dt * 1.6;
            sprayVelocity[i * 3 + 1] -= dt * 0.15;
            sprayPositions[i * 3] += sprayVelocity[i * 3] * dt;
            sprayPositions[i * 3 + 1] += sprayVelocity[i * 3 + 1] * dt;
            sprayPositions[i * 3 + 2] += sprayVelocity[i * 3 + 2] * dt;
        }
        if (alive > 0 || sprayWasAlive || snap) sprayGeometry.attributes.position.needsUpdate = true;
        sprayWasAlive = alive > 0;
        boothLight.intensity = spraying ? 18 : 12;

        // Oven: burner flicker and the set point on the display.
        const flicker = 0.85 + Math.sin(time * 9) * 0.08 + Math.sin(time * 23) * 0.05;
        ovenLights.forEach((l) => { l.intensity = (inOven ? 9 : 5) * flicker; });
        glowMaterial.opacity = (inOven ? 0.95 : 0.7) * flicker;
        drawDisplay(inOven ? 180 + Math.round(Math.sin(time * 0.7) * 2) : 180);
        lamps[1].visible = !inOven || Math.sin(time * 6) > 0;

        // Camera drifts with the leading part; on narrow screens it follows it closely.
        const aspect = camera.aspect;
        const follow = THREE.MathUtils.clamp(1.55 - aspect * 0.6, 0.3, 0.95);
        const targetX = THREE.MathUtils.clamp(lead * follow, -7, aspect < 1 ? 9.4 : 7.5);
        camX = snap ? targetX : THREE.MathUtils.lerp(camX, targetX, 1 - Math.pow(0.0015, dt));
        const distance = aspect < 1 ? 13 : aspect < 1.4 ? 15 : 16.5;
        camera.position.set(camX + (aspect < 1 ? 1.2 : 3.2), 5.2, distance);
        lookAt.set(camX, 1.8, 0);
        camera.lookAt(lookAt);
        return alive > 0 || Math.abs(camX - targetX) > 0.002;
    };

    // ---------- Merge the static scene ----------
    // Everything that never moves is merged per material, which takes the draw calls from ~340 to a few dozen.
    // Parts, gun carriages, trolleys, the spray and the blinking lamp stay separate.
    {
        const buckets = new Map();
        for (const object of [...scene.children]) {
            if (!object.isMesh || object.isInstancedMesh || object.userData.dynamic) continue;
            const key = `${object.material.uuid}|${object.castShadow}|${object.receiveShadow}`;
            if (!buckets.has(key)) buckets.set(key, []);
            buckets.get(key).push(object);
        }
        for (const meshes of buckets.values()) {
            if (meshes.length < 2) continue;
            const geometries = meshes.map((m) => {
                m.updateMatrixWorld(true);
                let g = m.geometry.index ? m.geometry.toNonIndexed() : m.geometry.clone();
                g.applyMatrix4(m.matrixWorld);
                for (const name of Object.keys(g.attributes)) {
                    if (!["position", "normal", "uv"].includes(name)) g.deleteAttribute(name);
                }
                g.clearGroups();
                return g;
            });
            const merged = mergeGeometries(geometries, false);
            if (!merged) continue;
            const first = meshes[0];
            const combined = new THREE.Mesh(merged, first.material);
            combined.castShadow = first.castShadow;
            combined.receiveShadow = first.receiveShadow;
            scene.add(combined);
            for (const m of meshes) {
                scene.remove(m);
                m.geometry.dispose();
            }
            geometries.forEach((g) => g.dispose());
        }
    }

    // ---------- Sizing and the loop ----------
    const resize = () => {
        const w = stage.clientWidth;
        const h = stage.clientHeight;
        renderer.setPixelRatio(pixelRatio);
        renderer.setSize(w, h, false);
        camera.aspect = w / h;
        camera.fov = camera.aspect < 1 ? 42 : 32;
        camera.updateProjectionMatrix();
    };
    // Frames are drawn only while something changes: during scrolling, for a moment after it, while powder is
    // still in the air and while the camera settles. Otherwise the GPU idles.
    let running = false;
    let visible = true;
    let animationTime = 0;
    let lastFrame = 0;
    let lastActivity = performance.now();
    let busy = false;
    const frameTimes = [];
    let slowWindows = 0;
    const render = (dt, snap, at) => {
        progress = at === undefined ? readProgress() : at;
        animationTime += dt;
        busy = update(animationTime, dt, snap);
        renderer.render(scene, camera);
    };
    // Adaptive resolution: average the last 30 frames; drop the pixel ratio when they run slower than ~45 fps,
    // raise it back when there is headroom.
    const adapt = (frameMs) => {
        frameTimes.push(frameMs);
        if (frameTimes.length < 30) return;
        const average = frameTimes.reduce((a, b) => a + b, 0) / frameTimes.length;
        frameTimes.length = 0;
        slowWindows = average > 22 ? slowWindows + 1 : 0;
        let next = pixelRatio;
        if (slowWindows >= 2 && pixelRatio > 0.75) {
            next = Math.max(0.75, pixelRatio - 0.15);
            slowWindows = 0;
        }
        else if (average < 13 && pixelRatio < maxPixelRatio) next = Math.min(maxPixelRatio, pixelRatio + 0.1);
        if (next !== pixelRatio) {
            pixelRatio = next;
            resize();
        }
    };
    const loop = (now) => {
        const frameMs = lastFrame ? now - lastFrame : 16;
        lastFrame = now;
        if (!visible || (!busy && now - lastActivity > 1200)) {
            running = false;
            lastFrame = 0;
            frameTimes.length = 0;
            return;
        }
        render(Math.min(frameMs / 1000, 0.05), false);
        // Only steady frames count; the first one after a pause measures the pause.
        if (frameMs < 250) adapt(frameMs);
        window.requestAnimationFrame(loop);
    };
    const wake = () => {
        lastActivity = performance.now();
        if (visible && !running && !reducedMotion) {
            running = true;
            window.requestAnimationFrame(loop);
        }
    };

    section.classList.add("is-3d");
    if (!reducedMotion) section.classList.add("is-live");
    resize();
    // Warm up: draw the line at a few points of the journey so every shader variant (bare, coated, cured parts,
    // spray, shadows) is compiled now instead of mid-scroll.
    renderer.compile(scene, camera);
    if (!reducedMotion) [0.15, 0.3, 0.45, 0.7, 1].forEach((at) => render(0, true, at));
    render(0, true);
    new ResizeObserver(() => { resize(); if (!running) render(0, true); }).observe(stage);
    if (reducedMotion) return;

    window.addEventListener("scroll", wake, { passive: true });
    new IntersectionObserver((entries) => {
        visible = entries[0].isIntersecting;
        if (visible) wake();
    }).observe(section);
}
