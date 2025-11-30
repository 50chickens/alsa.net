#include <stdio.h>
#include <stdlib.h>
#include <alsa/asoundlib.h>

int main(void)
{
    int err;
    int card = -1;

    printf("Starting C ALSA debug program...\n");
    err = snd_card_next(&card);
    if (err < 0) {
        fprintf(stderr, "snd_card_next error: %s\n", snd_strerror(err));
        return 1;
    }
    printf("snd_card_next returned %d, card=%d\n", err, card);

    while (card >= 0) {
        printf("About to call snd_card_get_name for card=%d\n", card);
        char *name = NULL;
        int r = snd_card_get_name(card, &name);
        if (r == 0 && name) {
            printf("snd_card_get_name returned '%s'\n", name);
            free(name);
        } else {
            printf("snd_card_get_name returned error %d\n", r);
        }

        char *longname = NULL;
        r = snd_card_get_longname(card, &longname);
        if (r == 0 && longname) {
            printf("snd_card_get_longname returned '%s'\n", longname);
            free(longname);
        } else {
            printf("snd_card_get_longname returned error %d\n", r);
        }

        // Try mixer sequence
        snd_mixer_t *mixer = NULL;
        char attachName[128];
        snprintf(attachName, sizeof(attachName), "hw:%d", card);
        printf("Attempting snd_mixer_open / attach '%s'\n", attachName);
        err = snd_mixer_open(&mixer, 0);
        printf("snd_mixer_open -> %d, mixer=%p\n", err, (void*)mixer);
        if (err >= 0) {
            err = snd_mixer_attach(mixer, attachName);
            printf("snd_mixer_attach -> %d\n", err);
            err = snd_mixer_selem_register(mixer, NULL, NULL);
            printf("snd_mixer_selem_register -> %d\n", err);
            err = snd_mixer_load(mixer);
            printf("snd_mixer_load -> %d\n", err);
            snd_mixer_t *closeMixer = mixer;
            if (closeMixer) snd_mixer_close(closeMixer);
        }

        err = snd_card_next(&card);
        if (err < 0) break;
        printf("snd_card_next returned %d, card=%d\n", err, card);
    }

    printf("Done.\n");
    return 0;
}
