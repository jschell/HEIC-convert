# README Style Guide

A repeatable pattern for writing project READMEs. Prioritizes "I found this repo, now what?" — the reader should be able to install, run, and understand the tool within the first screenful.

Synthesized from reviewing repos by [Bob Rudis](https://codeberg.org/hrbrmstr).

## Principles

- Install-first. Respect the reader's time.
- Real examples with real values, not `<placeholder>` arguments.
- Tables over bullet lists for commands, features, and settings.
- Technical depth is deferred, not eliminated.
- No ceremony, no decoration.

## Section Order

| Section | Required | Notes |
|---------|----------|-------|
| `# Project Name` | Yes | Just the name |
| CI/Release badges | If available | Build and release status only, between name and one-liner |
| One-liner | Yes | Plain sentence: what it does and why it matters |
| `## Installation` | Yes | Always the first actionable section |
| `## Usage` | Yes | Command table + concrete examples |
| `## Features` | If needed | Table format preferred |
| `## Configuration` | If needed | Settings table with defaults |
| Technical details | If needed | Architecture, format specs — after the reader knows how to use it |
| `## Building` | If needed | For contributors. Keep brief. |
| `## License` | Yes | One line at the end |

## Tone

- Casual-technical. Contractions are fine.
- State what it does in plain English first.
- Call out intentional design decisions: "(deliberate design decision)" or "(open to debate)".
- No "made with..." footers, no emoji decoration, no author bios.

## Badges

If the project has CI/release workflows, keep the status badges between the project name and the one-liner. Limit to build and release status only — no version shields, download counters, or "awesome" badges.

## Template

```markdown
# Project Name

[![CI](https://github.com/<user>/<repo>/actions/workflows/ci.yml/badge.svg)](https://github.com/<user>/<repo>/actions/workflows/ci.yml)
[![Release](https://github.com/<user>/<repo>/actions/workflows/release.yml/badge.svg)](https://github.com/<user>/<repo>/actions/workflows/release.yml)

One plain sentence: what it does and why it matters to you.

## Installation

### Download

Download the latest release from [Releases](../../releases) and run it. No installation required.

### Build from Source

\```
git clone <url>
cd <project>
<build command>
\```

## Usage

| Command / Action | Purpose |
|------------------|---------|
| `cmd action1` | Does the main thing |
| `cmd action2` | Does the other thing |

\```
# Concrete example with real values
cmd action1 --flag actual-value
\```

## Configuration

Settings are stored in `<path>`.

| Setting | Default | Description |
|---------|---------|-------------|
| Setting A | `value` | What it controls |
| Setting B | `value` | What it controls |

## How It Works

Only if needed. Architecture, format specs, protocol details.
Keep it after the reader already knows how to install and use it.

## Project Structure

\```
project/
├── src/           # Source code
├── tests/         # Test suite
└── docs/          # Documentation
\```

## License

MIT
```

## Anti-Patterns

| Don't | Do Instead |
|-------|------------|
| Wall of badges at the top | CI/release status only, if available |
| Features as 20-item bullet list | Features as a table |
| `<placeholder>` in examples | Real values you can copy-paste |
| Technical details before install | Install and usage first |
| `####` deep nesting | Stay at `##` and `###` |
| "Contributing" boilerplate section | One sentence or omit |
| Full license text in README | Link to LICENSE file |
| Screenshots as hero images | Describe visually in text if needed |
