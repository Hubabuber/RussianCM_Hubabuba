"""CMU14: exercise hook-style submodule updates in disposable local repositories."""

import importlib.util
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch


class GitHelperWorktreeTests(unittest.TestCase):
    def setUp(self):
        source = Path(__file__).resolve().parent / "git_helper.py"
        workspace = source.parent.parent
        scratch = workspace / ".codex-build"
        scratch.mkdir(exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(prefix="buildchecker regression ", dir=scratch)
        self.root = Path(self.temporary.name).resolve()
        self.root.relative_to(workspace.resolve())
        self.addCleanup(self.temporary.cleanup)
        environment = dict(os.environ)
        environment.pop("GITHUB_ACTIONS", None)
        patcher = patch.dict(os.environ, environment, clear=True)
        patcher.start()
        self.addCleanup(patcher.stop)
        self.environment = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
        self.environment.update(GIT_ALLOW_PROTOCOL="file", GIT_TERMINAL_PROMPT="0",
                                GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)
        spec = importlib.util.spec_from_file_location("git_helper_under_test", source)
        self.helper = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.helper)
        self.calls = []

    def git(self, directory, *arguments):
        result = subprocess.run(
            ["git", "-C", str(directory), *arguments], env=self.environment,
            check=True, capture_output=True, text=True,
        )
        return result.stdout.strip()

    def commit(self, directory):
        self.git(directory, "add", "--all")
        self.git(directory, "-c", "user.name=BuildChecker regression", "-c",
                 "user.email=buildchecker@example.invalid", "commit", "-m", "Fixture", "--no-verify")

    def repository(self):
        dependency = self.root / "dependency"
        dependency.mkdir()
        self.git(dependency, "init")
        (dependency / "fixture.txt").write_text("local submodule fixture\n", encoding="utf-8")
        self.commit(dependency)
        repository = self.root / "repository"
        repository.mkdir()
        self.git(repository, "init")
        self.git(repository, "submodule", "add", str(dependency), "RobustToolbox")
        self.commit(repository)
        return repository

    def update_from_hook(self, repository):
        checker = repository / "BuildChecker"
        checker.mkdir(exist_ok=True)
        self.helper.__file__ = str(checker / "git_helper.py")
        hook_environment = dict(self.environment)
        hook_environment.update(GIT_DIR=self.git(repository, "rev-parse", "--absolute-git-dir"), GIT_WORK_TREE=".")

        def run_command(command, capture=False):
            self.calls.append(command)
            return subprocess.run(command, cwd=checker, env=hook_environment,
                                  check=True, capture_output=True, text=True)

        self.helper.run_command = run_command
        self.helper.update_submodules()
        self.assertEqual((repository / "RobustToolbox" / "fixture.txt").read_text(encoding="utf-8"),
                         "local submodule fixture\n")
        self.assertFalse((checker / "RobustToolbox").exists())
        self.assertEqual(len(self.calls), 1)

    def test_hook_keeps_submodule_at_regular_checkout_root(self):
        self.update_from_hook(self.repository())

    def test_hook_initializes_submodule_at_linked_worktree_root(self):
        repository = self.repository()
        linked = self.root / "linked worktree"
        self.git(repository, "worktree", "add", "--detach", str(linked), "HEAD")
        self.update_from_hook(linked)

    def test_disable_marker_uses_helper_directory(self):
        checker = self.root / "BuildChecker"
        checker.mkdir()
        (checker / "DISABLE_SUBMODULE_AUTOUPDATE").touch()
        self.helper.__file__ = str(checker / "git_helper.py")
        self.helper.run_command = lambda *args, **kwargs: self.fail("Disabled update called Git")
        self.helper.update_submodules()

    def test_github_actions_skips_submodule_update(self):
        self.helper.run_command = lambda *args, **kwargs: self.fail("CI update called Git")
        with patch.dict(os.environ, {"GITHUB_ACTIONS": "true"}):
            self.helper.update_submodules()


if __name__ == "__main__":
    unittest.main()
