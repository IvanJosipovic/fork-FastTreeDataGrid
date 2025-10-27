# FastTreeDataGrid Prototype Plan

## Milestone 1 — Project Setup
1. [x] Verify toolchain and establish repository conventions.
2. [x] Scaffold Avalonia solution and baseline projects.
3. [x] Introduce shared infrastructure folder structure (src/, samples/, docs/).

## Milestone 2 — Core Control Skeleton
4. [x] Define `FastTreeDataGrid` control shell with canvas-based visual tree.
5. [x] Implement reusable column definition model supporting Auto/Pixel/Star sizing.
6. [x] Create layout coordinator to map logical rows/columns to pixel positions.

## Milestone 3 — Virtualization Mechanics
7. [x] Build row virtualization manager that recycles row presenters.
8. [x] Implement cell presenter pooling and placement within rows.
9. [x] Connect virtualization to data source abstraction (tree hierarchy adapter).

## Milestone 4 — Interaction & Integration
10. [x] Render headers via canvas and synchronize with columns.
11. [x] Wire scrolling/viewport updates and ensure smooth repositioning.
12. [x] Implement minimal selection & expand/collapse interactions.

## Milestone 5 — Demo & Documentation
13. [x] Assemble demo data/model and host control in sample window.
14. [x] Validate core scenarios (scrolling, resize, hierarchy) in demo.
15. [x] Document usage notes and next steps in README.

## Milestone 6 — Flat Data & Widget Rendering
16. [x] Rework data source to operate on a flat tree list for fast sorting/filtering.
17. [x] Port lightweight widget rendering host and integrate into control library.
18. [x] Replace cell containers with widget hosts using a single widget type per column.
19. [x] Update demo to consume new flat source and showcase widget-based cells.
