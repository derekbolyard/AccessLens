import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MagicAuthModal } from './magic-auth-modal';

describe('MagicAuthModal', () => {
  let component: MagicAuthModal;
  let fixture: ComponentFixture<MagicAuthModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MagicAuthModal]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MagicAuthModal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
