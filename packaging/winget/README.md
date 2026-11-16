# winget paketi (`devutku.Akinci`)

Akıncı'nın [winget](https://learn.microsoft.com/windows/package-manager/) manifest'leri. Paket kabul edildikten sonra kullanıcılar şu komutla kurabilir:

```powershell
winget install devutku.Akinci
```

`manifests/` klasörü, [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) deposundaki düzenin aynısıdır (`manifests/d/devutku/Akinci/<sürüm>/`).

## İlk gönderim

1. Manifest'leri doğrulayın:
   ```powershell
   winget validate --manifest packaging\winget\manifests\d\devutku\Akinci\1.1.1
   ```
2. `microsoft/winget-pkgs` deposunu fork'layın, `manifests/d/devutku/Akinci/1.1.1/` klasörünü fork'a kopyalayın ve `microsoft/winget-pkgs`'e pull request açın.
3. winget-pkgs'in otomatik doğrulaması kurulum programını sanal makinede kurup kaldırır; bir moderatör onaylayınca paket yayına girer.

## Yeni sürüm

Her sürümde değişen yalnızca: `PackageVersion`, `InstallerUrl`, `InstallerSha256`, `ReleaseDate` ve `ReleaseNotesUrl`.
`InstallerSha256` değerini kurulum programını indirmeden GitHub'dan alabilirsiniz:

```bash
gh api repos/devutku/akinci/releases/tags/v<sürüm> --jq '.assets[] | select(.name|startswith("Akinci-Setup")) | .digest'
```

`ProductCode` (`{4C13BB6C-AE62-472E-B15A-DC329B2C10AF}_is1`) kurulum programının sabit `AppId` değerinden gelir ve değişmez.
